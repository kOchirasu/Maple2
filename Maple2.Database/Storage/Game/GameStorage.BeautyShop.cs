using System.Linq;
using Maple2.Model.Game.Shop;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public BeautyShop? GetBeautyShop(int shopId) {
            BeautyShop? beautyShop = Context.BeautyShop.Find(shopId);
            if (beautyShop == null) {
                return null;
            }

            // Get shop entries
            beautyShop.Entries = Context.BeautyShopEntry.Where(model => model.ShopId == shopId)
                .Select<Model.Shop.BeautyShopEntry, BeautyShopEntry>(item => item)
                .ToList();
            return beautyShop;
        }
    }
}
