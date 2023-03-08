using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Serilog;

namespace Maple2.Server.Game.Manager;

public class ItemDropManager {
    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required ItemMetadataStorage ItemMetadata { private get; init; }
    public required TableMetadataStorage TableMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    private readonly GameSession session;
    public int BoxCount;
    private readonly ILogger logger = Log.Logger.ForContext<ItemDropManager>();

    public ItemDropManager(GameSession session) {
        this.session = session;
        BoxCount = 0;
    }

    public void Reset() {
        BoxCount = 0;
    }
    
    public ItemBoxError HandleSelectItemBox(int index, Item item, int count = 1) {
        var error = ItemBoxError.ok;

        int[] selectItemBoxParams = item.Metadata.Function?.Parameters.Split(',').Select(int.Parse).ToArray() ?? Array.Empty<int>();
        if (selectItemBoxParams.Length < 2) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }
        
        if(session.Item.Inventory.Find(item.Id).Sum(box => box.Amount) < selectItemBoxParams[0]) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        if (!TableMetadata.IndividualItemDropTable.Entries.TryGetValue(selectItemBoxParams[1], out Dictionary<byte, IList<IndividualItemDropTable.Entry>>? dropGroupTable)) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }
        
        // SelectItemBox disregards DropGroup
        IList<IndividualItemDropTable.Entry> drops = session.ItemDrop.FilterDrops(dropGroupTable.Values.SelectMany(x => x).ToList());
        
        if (drops.Count == 0) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }
        
        IndividualItemDropTable.Entry selectedEntry = drops[index];

        for (int startCount = 0; startCount < count; startCount++) {
            if (!session.Item.Inventory.ConsumeItemComponents(new List<ItemComponent>() {
                    new(item.Id, item.Rarity, selectItemBoxParams[0], ItemTag.None),
                })) {
                return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
            }

            IEnumerable<Item> itemList = session.ItemDrop.GetItemsFromGroup(item, selectedEntry);
            foreach(Item newItem in itemList) {
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

    public ItemBoxError HandleOpenItemBox(Item item, int count = 1) {
        var error = ItemBoxError.ok;
        int[] itemBoxParams = item.Metadata.Function?.Parameters.Split(',').Select(int.Parse).ToArray() ?? Array.Empty<int>();
        // params:
        // 0 = RequiredItemId
        // 1 = Receive one item from each drop group (bool)
        // 2 = unk?
        // 3 = BoxId
        // 4 (if present) = Amount (of boxes) required, otherwise it's always 1

        switch (itemBoxParams.Length) {
            case < 4:
            case 5 when session.Item.Inventory.Find(item.Id).Sum(box => box.Amount) < itemBoxParams[4]:
                return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        if (itemBoxParams[0] > 0 && session.Item.Inventory.Find(item.Id).Sum(requiredItem => requiredItem.Amount) < itemBoxParams[0]) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // Get dropbox
        if (!TableMetadata.IndividualItemDropTable.Entries.TryGetValue(itemBoxParams[3], out Dictionary<byte, IList<IndividualItemDropTable.Entry>>? dropGroupTable)) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        if (itemBoxParams[1] == 1) {
            for (int startCount = 0; startCount < count; startCount++) {
                foreach ((byte dropGroup, IList<IndividualItemDropTable.Entry> drops) in dropGroupTable) {
                    IList<IndividualItemDropTable.Entry> filteredDrops = session.ItemDrop.FilterDrops(drops);

                    // randomize contents
                    IndividualItemDropTable.Entry selectedEntry = filteredDrops[Random.Shared.Next(0, filteredDrops.Count)];
                    IEnumerable<Item> itemList = session.ItemDrop.GetItemsFromGroup(item, selectedEntry);
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
            // get every item in every drop group
            IList<IndividualItemDropTable.Entry> drops = session.ItemDrop.FilterDrops(dropGroupTable.Values.SelectMany(x => x).ToList());
            for (int startCount = 0; startCount < count; startCount++) {
                foreach (IndividualItemDropTable.Entry drop in drops) {
                    IEnumerable<Item> itemList = session.ItemDrop.GetItemsFromGroup(item, drop);
                    foreach (Item newItem in itemList) {
                        if (!session.Item.Inventory.Add(newItem, true)) {
                            error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                        }
                        BoxCount++;
                        if (error != ItemBoxError.ok) {
                            return error;
                        }
                    }
                }
            }
        }
        return error;
    }

    public IList<IndividualItemDropTable.Entry> FilterDrops(IEnumerable<IndividualItemDropTable.Entry> entries) {
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

    public IEnumerable<Item> GetItemsFromGroup(Item boxItem, IndividualItemDropTable.Entry entry) {
        var items = new List<Item>();
        foreach (int id in entry.ItemIds) {
            if (!ItemMetadata.TryGet(id, out ItemMetadata? itemMetadata)) {
                continue;
            }

            var newItem = new Item(itemMetadata, entry.Rarity ?? boxItem.Metadata.Option?.ConstantId ?? 1, Random.Shared.Next(entry.MinCount, entry.MaxCount));
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
}
