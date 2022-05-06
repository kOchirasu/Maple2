using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Item CreateItem(long ownerId, Item item) {
            Model.Item model = item;
            model.OwnerId = ownerId;
            model.Id = 0;
            context.Item.Add(model);

            return context.TrySaveChanges() ? model.Convert(game.itemMetadata.Get(model.ItemId)) : null;
        }

        public Item SplitItem(long ownerId, Item item, int amount) {
            Model.Item model = item;
            model.Amount = amount;
            model.OwnerId = ownerId;
            model.Slot = -1;
            model.EquipSlot = EquipSlot.Unknown;
            model.EquipTab = EquipTab.None;
            model.Id = 0;
            context.Item.Add(model);

            return context.TrySaveChanges() ? model.Convert(game.itemMetadata.Get(model.ItemId)) : null;
        }

        public List<Item> CreateItems(long ownerId, params Item[] items) {
            var models = new Model.Item[items.Length];
            for (int i = 0; i < items.Length; i++) {
                models[i] = items[i];
                models[i].OwnerId = ownerId;
                models[i].Id = 0;
                context.Item.Add(models[i]);
            }

            if (!context.TrySaveChanges()) {
                return null;
            }

            return models.Select(model => model.Convert(game.itemMetadata.Get(model.ItemId))).ToList();
        }

        public Item GetItem(long itemUid) {
            Model.Item model = context.Item.Find(itemUid);
            return model?.Convert(game.itemMetadata.Get(model.ItemId));
        }

        public IDictionary<EquipTab, List<Item>> GetEquips(long characterId, params EquipTab[] tab) {
            return context.Item.Where(item => item.OwnerId == characterId && tab.Contains(item.EquipTab))
                .AsEnumerable()
                .GroupBy(item => item.EquipTab)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(model => model.Convert(game.itemMetadata.Get(model.ItemId))).ToList()
                );
        }

        public Dictionary<InventoryType, List<Item>> GetInventory(long characterId) {
            return context.Item.Where(item => item.OwnerId == characterId && item.EquipTab == EquipTab.None)
                .Select(model => model.Convert(game.itemMetadata.Get(model.ItemId)))
                .AsEnumerable()
                .GroupBy(item => item.Inventory)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );
        }

        public List<Item> GetItems(long characterId) {
            return context.Item.Where(item => item.OwnerId == characterId)
                .Select(model => model.Convert(game.itemMetadata.Get(model.ItemId)))
                .ToList();
        }

        public bool SaveItems(long ownerId, params Item[] items) {
            var models = new Model.Item[items.Length];
            for (int i = 0; i < items.Length; i++) {
                models[i] = items[i];
                models[i].OwnerId = ownerId;
                context.Item.Update(models[i]);
            }

            return context.TrySaveChanges();
        }
    }
}
