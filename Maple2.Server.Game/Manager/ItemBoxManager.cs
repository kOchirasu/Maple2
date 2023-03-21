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
    private readonly GameSession session;
    public int BoxCount { get; private set; }

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
            ItemFunction.SelectItemBox => SelectItemBox(item, itemBoxParams[0], itemBoxParams[1], index, count),
            ItemFunction.OpenItemBox => OpenItemBox(item, itemBoxParams[0], itemBoxParams[1], itemBoxParams[2], itemBoxParams[3], itemBoxParams.Length == 5 ? itemBoxParams[4] : 1, count),
            ItemFunction.OpenItemBoxWithKey => OpenItemBoxWithKey(item, itemBoxParams[0], itemBoxParams[1], itemBoxParams[2], itemBoxParams[5], count),
            _ => throw new ArgumentOutOfRangeException(item.Metadata.Function?.Type.ToString(), "Invalid box type"),
        };
    }

    private ItemBoxError SelectItemBox(Item item, int itemRequiredAmount, int boxId, int index, int count = 1) {
        var error = ItemBoxError.ok;

        // check if box count is enough to open
        if (session.Item.Inventory.Find(item.Id).Sum(box => box.Amount) < itemRequiredAmount) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // check if we can get metadata
        if (!session.TableMetadata.IndividualItemDropTable.Entries.TryGetValue(boxId, out Dictionary<byte, IList<IndividualItemDropTable.Entry>>? dropGroupTable)) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // SelectItemBox disregards DropGroup
        IList<IndividualItemDropTable.Entry> drops = session.ItemBox.FilterDrops(dropGroupTable.Values.SelectMany(x => x).ToList());


        IndividualItemDropTable.Entry? selectedEntry = drops.ElementAtOrDefault(index);
        if (selectedEntry == null) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        ItemComponent ingredient = new(item.Id, -1, itemRequiredAmount, ItemTag.None);
        for (int startCount = 0; startCount < count; startCount++) {
            if (!session.Item.Inventory.ConsumeItemComponents(new[] {ingredient})) {
                return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
            }

            IEnumerable<Item> itemList = session.ItemBox.GetItemsFromGroup(selectedEntry);
            foreach (Item newItem in itemList) {
                if (!session.Item.Inventory.Add(newItem, true, true)) {
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

    private ItemBoxError OpenItemBox(Item item, int globalDropBoxId, int unknownId, int itemId, int boxId, int itemRequiredAmount, int count = 1) {
        var error = ItemBoxError.ok;
        // if globalDropBoxId > 0, unknownId is possibly another globalDropBoxId. Not confirmed, just a guess.
        // otherwise unknownId is treated as a bool. if 1 = receive one item from each drop group, if 0 = receive all items from one drop group

        if (boxId == 0) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        if (itemRequiredAmount > -1 && session.Item.Inventory.Find(item.Id).Sum(box => box.Amount) < itemRequiredAmount) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // TODO: branch off to globalDropBoxId. Temp solution
        if (globalDropBoxId > 0) {
            return HandleGlobalDropBox(item, itemId, itemRequiredAmount, count);
        }

        // Get dropbox
        if (!session.TableMetadata.IndividualItemDropTable.Entries.TryGetValue(boxId, out Dictionary<byte, IList<IndividualItemDropTable.Entry>>? dropGroupTable)) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        if (unknownId == 1) {
            ItemComponent ingredient = new(item.Id, item.Rarity, itemRequiredAmount, ItemTag.None);
            for (int startCount = 0; startCount < count; startCount++) {
                if (!session.Item.Inventory.ConsumeItemComponents(new[] {ingredient})) {
                    return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
                }

                if (itemId > 0 && session.ItemMetadata.TryGet(itemId, out ItemMetadata? itemMetadata)) {
                    if (!session.Item.Inventory.Add(new Item(itemMetadata, item.Metadata.Option?.ConstantId ?? 1), true, true)) {
                        error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                    }
                }

                foreach ((byte dropGroup, IList<IndividualItemDropTable.Entry> drops) in dropGroupTable) {
                    IList<IndividualItemDropTable.Entry> filteredDrops = session.ItemBox.FilterDrops(drops);

                    // randomize contents
                    IndividualItemDropTable.Entry selectedEntry = filteredDrops[Random.Shared.Next(0, filteredDrops.Count)];
                    IEnumerable<Item> itemList = session.ItemBox.GetItemsFromGroup(selectedEntry);
                    foreach (Item newItem in itemList) {
                        if (!session.Item.Inventory.Add(newItem, true, true)) {
                            error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                        }
                    }
                }
                BoxCount++;
                if (error != ItemBoxError.ok) {
                    return error;
                }
            }
        } else {
            error = GiveAllDropBoxItems(item, itemId, itemRequiredAmount, count, dropGroupTable);
        }
        return error;
    }
    private ItemBoxError GiveAllDropBoxItems(Item item, int itemId, int itemRequiredAmount, int count, Dictionary<byte, IList<IndividualItemDropTable.Entry>> dropGroupTable) {
        var error = ItemBoxError.ok;
        // get every item in every drop group
        IList<IndividualItemDropTable.Entry> drops = session.ItemBox.FilterDrops(dropGroupTable.Values.SelectMany(x => x).ToList());
        ItemComponent ingredient = new(item.Id, item.Rarity, itemRequiredAmount, ItemTag.None);
        for (int startCount = 0; startCount < count; startCount++) {
            if (!session.Item.Inventory.ConsumeItemComponents(new[] {ingredient})) {
                return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
            }

            if (itemId > 0 && session.ItemMetadata.TryGet(itemId, out ItemMetadata? itemMetadata)) {
                if (!session.Item.Inventory.Add(new Item(itemMetadata), true, true)) {
                    error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                }
            }
            foreach (IndividualItemDropTable.Entry drop in drops) {
                IEnumerable<Item> itemList = session.ItemBox.GetItemsFromGroup(drop);
                foreach (Item newItem in itemList) {
                    if (!session.Item.Inventory.Add(newItem, true, true)) {
                        error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory;
                    }
                }
            }
            BoxCount++;
            if (error != ItemBoxError.ok) {
                return error;
            }
        }
        return error;
    }

    private ItemBoxError OpenItemBoxWithKey(Item item, int keyItemId, int keyAmountRequired, int itemId, int boxId, int count = 1) {
        if (keyItemId == 0) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // check for keys
        if (session.Item.Inventory.Find(keyItemId).Sum(requiredItem => requiredItem.Amount) < keyAmountRequired) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }

        // Get dropbox
        if (!session.TableMetadata.IndividualItemDropTable.Entries.TryGetValue(boxId, out Dictionary<byte, IList<IndividualItemDropTable.Entry>>? dropGroupTable)) {
            return ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
        }
        return GiveAllDropBoxItems(item, itemId, keyAmountRequired, count, dropGroupTable);
    }

    private IList<IndividualItemDropTable.Entry> FilterDrops(IEnumerable<IndividualItemDropTable.Entry> entries) {
        var filteredEntries = new List<IndividualItemDropTable.Entry>();
        foreach (IndividualItemDropTable.Entry entry in entries) {
            if (!session.ItemMetadata.TryGet(entry.ItemIds[0], out ItemMetadata? itemMetadata)) {
                continue;
            }

            // Check for any job restrictions
            if (entry.SmartDropRate > 0 && itemMetadata.Limit.JobRecommends.Length > 0 &&
                !itemMetadata.Limit.JobRecommends.Contains(session.Player.Value.Character.Job.Code()) && !itemMetadata.Limit.JobRecommends.Contains(JobCode.None)) {
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
            if (!session.ItemMetadata.TryGet(id, out ItemMetadata? itemMetadata)) {
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

    private ItemBoxError HandleGlobalDropBox(Item item, int itemId, int itemRequiredAmount, int count) {
        var error = ItemBoxError.ok;
        ItemComponent ingredient = new(item.Id, item.Rarity, itemRequiredAmount, ItemTag.None);
        for (int startCount = 0; startCount < count; startCount++) {
            // assumes itemBoxParams[1] is another globalDropBoxId utilized
            if (!session.Item.Inventory.ConsumeItemComponents(new[] {ingredient})) {
                error = ItemBoxError.s_err_cannot_open_multi_itembox_inventory_fail;
            }

            if (itemId > 0 && session.ItemMetadata.TryGet(itemId, out ItemMetadata? itemMetadata)) {
                if (!session.Item.Inventory.Add(new Item(itemMetadata), true, true)) {
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
