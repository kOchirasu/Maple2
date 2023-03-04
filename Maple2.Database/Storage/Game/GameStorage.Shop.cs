using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Game.Shop;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Shop? GetShop(int shopId) => Context.Shop.Find(shopId);

        public IList<ShopItem> GetShopItems(int shopId) {
            return Context.ShopItem.Where(model => model.ShopId == shopId)
                .Select<Model.Shop.ShopItem, ShopItem>(item => item)
                .ToList();
        }
    }
}
