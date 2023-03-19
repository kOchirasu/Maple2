using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager;

public class ItemBoxManager {
    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required ItemMetadataStorage ItemMetadata { private get; init; }
    public required TableMetadataStorage TableMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    private readonly GameSession session;
    public int BoxCount;

    public ItemBoxManager(GameSession session) {
        this.session = session;
        BoxCount = 0;
    }

    public void Reset() {
        BoxCount = 0;
    }

    public ItemBoxError Open(Item item, int count = 1, int index = 0) {
        int[] itemBoxParams = item.Metadata.Function?.Parameters.Split(',').Select(int.Parse).ToArray() ?? Array.Empty<int>();
        return item.Metadata.Function?.Type switch {
            ItemFunction.SelectItemBox => SelectItemBox(item, itemBoxParams, index, count),
            ItemFunction.OpenItemBox => OpenItemBox(item, itemBoxParams, count),
            ItemFunction.OpenItemBoxWithKey => OpenItemBoxWithKey(item, itemBoxParams, count),
            _ => throw new ArgumentOutOfRangeException(item.Metadata.Function?.Type.ToString(), "Invalid box type"),
        };
    }

    private ItemBoxError SelectItemBox(Item item, int[] itemBoxParams, int index, int count = 1) {
        var error = ItemBoxError.ok;

        if (itemBoxParams.Length < 2) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // check if box count is enough to open
        if (session.Item.Inventory.Find(item.Id).Sum(box => box.Amount) < itemBoxParams[0]) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // check if we can get metadata
        if (!TableMetadata.IndividualItemDropTable.Entries.TryGetValue(itemBoxParams[1], out Dictionary<byte, IList<IndividualItemDropTable.Entry>>? dropGroupTable)) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // SelectItemBox disregards DropGroup
        IList<IndividualItemDropTable.Entry> drops = session.ItemBox.FilterDrops(dropGroupTable.Values.SelectMany(x => x).ToList());

        if (drops.Count == 0) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        IndividualItemDropTable.Entry selectedEntry = drops[index];
        for (int startCount = 0; startCount < count; startCount++) {
            if (!session.Item.Inventory.ConsumeItemComponents(new List<ItemComponent>() {
                    new(item.Id, -1, itemBoxParams[0], ItemTag.None),
                })) {
                return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
            }

            IEnumerable<Item> itemList = session.ItemBox.GetItemsFromGroup(selectedEntry);
            foreach (Item newItem in itemList) {
                if (!session.Item.Inventory.Add(newItem, true)) {
                    // TODO: Mail user
                    error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                }
            }
            BoxCount++;
            if (error != ItemBoxError.ok) {
                return error;
            }
        }
        return error;
    }

    private ItemBoxError OpenItemBox(Item item, int[] itemBoxParams, int count = 1) {
        var error = ItemBoxError.ok;
        // params:
        // 0 = globalDropBoxId if > 0, the param 1 is changed to something else, possibly another globalDropBoxId. Not confirmed, just a guess.
        // 1 = Receive one item from each drop group (bool)
        // 2 = ItemId
        // 3 = BoxId
        // 4 (if present) = Amount (of boxes) required, otherwise it's always 1

        switch (itemBoxParams.Length) {
            case < 4:
            case 5 when session.Item.Inventory.Find(item.Id).Sum(box => box.Amount) < itemBoxParams[4]:
                return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // TODO: branch off to globalDropBoxId. Temp solution
        if (itemBoxParams[0] > 0) {
            return HandleGlobalDropBox(item, itemBoxParams, count);
        }
        
        // Get dropbox
        if (!TableMetadata.IndividualItemDropTable.Entries.TryGetValue(itemBoxParams[3], out Dictionary<byte, IList<IndividualItemDropTable.Entry>>? dropGroupTable)) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        if (itemBoxParams[1] == 1) {
            for (int startCount = 0; startCount < count; startCount++) {
                if (!session.Item.Inventory.ConsumeItemComponents(new List<ItemComponent> {
                        new(item.Id, item.Rarity, itemBoxParams.Length == 5 ? itemBoxParams[5] : 1, ItemTag.None),
                    })) {
                    return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
                }
                
                if (itemBoxParams[2] > 0 && ItemMetadata.TryGet(itemBoxParams[2], out ItemMetadata? itemMetadata)) {
                    if (!session.Item.Inventory.Add(new Item(itemMetadata, item.Metadata.Option?.ConstantId ?? 1), true)) {
                        // TODO: Mail user
                        error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                    }
                }
                
                foreach ((byte dropGroup, IList<IndividualItemDropTable.Entry> drops) in dropGroupTable) {
                    IList<IndividualItemDropTable.Entry> filteredDrops = session.ItemBox.FilterDrops(drops);

                    // randomize contents
                    IndividualItemDropTable.Entry selectedEntry = filteredDrops[Random.Shared.Next(0, filteredDrops.Count)];
                    IEnumerable<Item> itemList = session.ItemBox.GetItemsFromGroup(selectedEntry);
                    foreach (Item newItem in itemList) {
                        if (!session.Item.Inventory.Add(newItem, true)) {
                            error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                        }
                    }
                    BoxCount++;
                    if (error != ItemBoxError.ok) {
                        return error;
                    }
                }
            }
        } else {
            error = GiveAllDropBoxItems(item, itemBoxParams, count, dropGroupTable);
        }
        return error;
    }
    private ItemBoxError GiveAllDropBoxItems(Item item, int[] itemBoxParams, int count, Dictionary<byte, IList<IndividualItemDropTable.Entry>> dropGroupTable) {
        var error = ItemBoxError.ok;
        // get every item in every drop group
        IList<IndividualItemDropTable.Entry> drops = session.ItemBox.FilterDrops(dropGroupTable.Values.SelectMany(x => x).ToList());
        for (int startCount = 0; startCount < count; startCount++) {
            if (!session.Item.Inventory.ConsumeItemComponents(new List<ItemComponent> {
                    new(item.Id, item.Rarity, itemBoxParams.Length == 5 ? itemBoxParams[5] : 1, ItemTag.None),
                })) {
                return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
            }

            if (itemBoxParams[2] > 0 && ItemMetadata.TryGet(itemBoxParams[2], out ItemMetadata? itemMetadata)) {
                if (!session.Item.Inventory.Add(new Item(itemMetadata), true)) {
                    // TODO: Mail user
                    error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                }
            }
            foreach (IndividualItemDropTable.Entry drop in drops) {
                IEnumerable<Item> itemList = session.ItemBox.GetItemsFromGroup(drop);
                foreach (Item newItem in itemList) {
                    if (!session.Item.Inventory.Add(newItem, true)) {
                        error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                    }
                    BoxCount++;
                }
                if (error != ItemBoxError.ok) {
                    return error;
                }
            }
        }
        return error;
    }

    private ItemBoxError OpenItemBoxWithKey(Item item, int[] itemBoxParams, int count = 1) {
        // params:
        // 0 = key Item Id
        // 1 = amount of keys required
        // 2 = unk
        // 3 = unk
        // 4 = unk
        // 5 = Box Id
        if (itemBoxParams.Length < 6 || itemBoxParams[0] == 0) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // check for keys
        if (session.Item.Inventory.Find(itemBoxParams[0]).Sum(requiredItem => requiredItem.Amount) < itemBoxParams[1]) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // Get dropbox
        return !TableMetadata.IndividualItemDropTable.Entries.TryGetValue(itemBoxParams[5], out Dictionary<byte, IList<IndividualItemDropTable.Entry>>? dropGroupTable) 
            ? ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail : GiveAllDropBoxItems(item, itemBoxParams, count, dropGroupTable);
    }

    private IList<IndividualItemDropTable.Entry> FilterDrops(IEnumerable<IndividualItemDropTable.Entry> entries) {
        var filteredEntries = new List<IndividualItemDropTable.Entry>();
        foreach (IndividualItemDropTable.Entry entry in entries) {
            if (!ItemMetadata.TryGet(entry.ItemIds[0], out ItemMetadata? itemMetadata)) {
                continue;
            }

            // Check for any job restrictions
            if (entry.SmartDropRate > 0 && itemMetadata.Limit.Jobs.Length > 0 && !itemMetadata.Limit.Jobs.Contains(session.Player.Value.Character.Job.Code())) {
                continue;
            }

            if (entry.SmartGender && itemMetadata.Limit.Gender != Gender.All && itemMetadata.Limit.Gender != session.Player.Value.Character.Gender) {
                continue;
            }
            filteredEntries.Add(entry);
        }
        return filteredEntries;
    }

    private IEnumerable<Item> GetItemsFromGroup(IndividualItemDropTable.Entry entry) {
        var items = new List<Item>();
        foreach (int id in entry.ItemIds) {
            if (!ItemMetadata.TryGet(id, out ItemMetadata? itemMetadata)) {
                continue;
            }

            int? rarity = entry.Rarity;
            if (rarity == null && itemMetadata.Option?.ConstantId < 6) {
                rarity = itemMetadata.Option?.ConstantId;
            }
            var newItem = new Item(itemMetadata, rarity ?? 1, Random.Shared.Next(entry.MinCount, entry.MaxCount));
            if (entry.ReduceRepackLimit && newItem.Transfer?.RemainRepackage > 0) {
                newItem.Transfer.RemainRepackage--;
            }

            // TODO: Apply enchant level & stats

            if (entry.ReduceTradeCount && newItem.Transfer?.RemainTrades > 0) {
                newItem.Transfer.RemainTrades--;
            }

            if (entry.Bind) {
                newItem.Transfer?.Bind(session.Player.Value.Character);
            }
            items.Add(newItem);
        }
        return items;
    }

    public ItemBoxError HandleGlobalDropBox(Item item, int[] itemBoxParams, int count) {
        var error = ItemBoxError.ok;
        for (int startCount = 0; startCount < count; startCount++) {
            // assumes itemBoxParams[1] is another globalDropBoxId utilized
            if (!session.Item.Inventory.ConsumeItemComponents(new List<ItemComponent> {
                    new(item.Id, item.Rarity, itemBoxParams.Length == 5 ? itemBoxParams[5] : 1, ItemTag.None),
                })) {
                error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
            }
                
            if (itemBoxParams[2] > 0 && ItemMetadata.TryGet(itemBoxParams[2], out ItemMetadata? itemMetadata)) {
                if (!session.Item.Inventory.Add(new Item(itemMetadata), true)) {
                    // TODO: Mail user
                    error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                }
                BoxCount++;
                if (error != ItemBoxError.ok) {
                    return error;
                }
            }
        }
        // return fail error for now until globalDropBox is implemented
        return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
    }
}
