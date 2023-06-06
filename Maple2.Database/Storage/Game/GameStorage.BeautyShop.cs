using System.Collections.Generic;
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
            foreach (BeautyShopEntry entry in Context.BeautyShopEntry.Where(model => model.ShopId == shopId)) {
                entry.Cost = beautyShop.ItemCost;
                beautyShop.Entries.Add(entry);
            }
            return beautyShop;
        }
        
        // TODO: Delete after. this is for debugging
        public void CreateBeautyShop(BeautyShop shop) {
            Model.Shop.BeautyShop model = shop;
            Context.BeautyShop.Add(model);
            Context.SaveChanges();
        }
    }
}
