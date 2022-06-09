using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Shop;

public sealed class BeautyShop : BeautyShopData {
    public readonly byte Unknown1;
    public readonly byte Unknown2;
    public readonly IList<BeautyShopEntry> Entries;

    public BeautyShop(int id, byte unknown1, byte unknown2) : base(id) {
        Unknown1 = unknown1;
        Unknown2 = unknown2;
        Entries = new List<BeautyShopEntry>();
    }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteByte(Unknown1);
        writer.WriteByte(Unknown2);
        writer.WriteShort((short) Entries.Count);
        foreach (BeautyShopEntry entry in Entries) {
            writer.WriteClass<BeautyShopEntry>(entry);
        }
    }

    #region temporary hardcoded shops
    public static BeautyShop Face() {
        var shop = new BeautyShop(500, 0, 0) {
            EntryType = 1,
            ShopType = 3,
            ShopSubType = 16,
            VoucherId = 20300036,
            RecolorCost = new BeautyShopCost(ShopCurrencyType.Meso, 10000),
        };
        shop.Entries.Add(new BeautyShopEntry(10300175, new BeautyShopCost(ShopCurrencyType.Meso, 9999)));
        return shop;
    }

    public static BeautyShop Skin() {
        return new BeautyShop(501, 0, 0) {
            EntryType = 3,
            ShopType = 4,
            ShopSubType = 19,
            VoucherId = 20300042,
            RecolorCost = new BeautyShopCost(ShopCurrencyType.EventMeret, 270),
        };
    }

    public static BeautyShop Hair() {
        var shop = new BeautyShop(504, 30, 1) {
            EntryType = 1,
            ShopType = 1,
            ShopSubType = 17,
            VoucherId = 20300035,
            RecolorCost = new BeautyShopCost(ShopCurrencyType.Meso, 10000),
        };
        shop.Entries.Add(new BeautyShopEntry(10200128, new BeautyShopCost(ShopCurrencyType.Meso, 9999)));
        return shop;
    }

    public static BeautyShop Cosmetic() {
        var shop = new BeautyShop(505, 0, 0) {
            EntryType = 1,
            ShopType = 2,
            ShopSubType = 20,
            VoucherId = 20300037,
            RecolorCost = new BeautyShopCost(ShopCurrencyType.Meso, 5000),
        };
        shop.Entries.Add(new BeautyShopEntry(10400000, new BeautyShopCost(ShopCurrencyType.Meso, 9999)));
        shop.Entries.Add(new BeautyShopEntry(10400222, new BeautyShopCost(ShopCurrencyType.Meso, 9999)));
        return shop;
    }

    public static BeautyShopData Dye() {
        return new BeautyShopData(506) {
            EntryType = 3,
            ShopType = 5,
            ShopSubType = 18,
            VoucherId = 20300038,
            RecolorCost = new BeautyShopCost(ShopCurrencyType.Meso, 10000),
        };
    }

    public static BeautyShop RandomHair() {
        var shop = new BeautyShop(508, 30, 2) {
            EntryType = 2,
            ShopType = 1,
            ShopSubType = 21,
            VoucherId = 20300244,
            ItemCost = new BeautyShopCost(ShopCurrencyType.EventMeret, 450),
            RecolorCost = new BeautyShopCost(ShopCurrencyType.EventMeret, 170),
        };
        shop.Entries.Add(new BeautyShopEntry(10200219, new BeautyShopCost(ShopCurrencyType.Meso, 9999)));
        return shop;
    }

    public static BeautyShop SpecialHair() {
        var shop = new BeautyShop(509, 30, 2) {
            EntryType = 1,
            ShopType = 1,
            ShopSubType = 0,
            VoucherId = 20300244,
            RecolorCost = new BeautyShopCost(20300246, 2),
        };
        shop.Entries.Add(new BeautyShopEntry(10200219, new BeautyShopCost(ShopCurrencyType.Meso, 9999)));
        return shop;
    }

    public static BeautyShopData SavedHair() {
        return new BeautyShopData(510) {
            EntryType = 4,
            ShopType = 1,
            ShopSubType = 20,
            VoucherId = 0,
            ItemCost = new BeautyShopCost(ShopCurrencyType.EventMeret, 10),
        };
    }
    #endregion
}
