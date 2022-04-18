using Maple2.Database.Extensions;
using Maple2.Model.Game;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Item CreateItem(Item item) {
            Model.Item model = item;
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
    }
}
