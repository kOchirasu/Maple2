using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Item CreateItem(Item item, long ownerId = -1) {
            Model.Item model = item;
            model.OwnerId = ownerId;
            model.Id = 0;
            context.Item.Add(model);
            if (!context.TrySaveChanges()) {
                return null;
            }

            return model.Convert(game.itemMetadata.Get(model.ItemId));
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

        public IList<Item> GetItems(long characterId) {
            return context.Item.Where(item => item.OwnerId == characterId)
                .Select(model => model.Convert(game.itemMetadata.Get(model.ItemId)))
                .ToList();
        }
        
        public IList<Item> GetInventory(long characterId) {
            return context.Item.Where(item => item.OwnerId == characterId && item.EquipTab == EquipTab.None)
                .Select(model => model.Convert(game.itemMetadata.Get(model.ItemId)))
                .ToList();
        }
    }
}
