using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game.Shop;

public class BeautyShopCost : IByteSerializable {
    public static BeautyShopCost Zero = new BeautyShopCost(ShopCurrencyType.Meso, 0);

    public readonly ShopCurrencyType Type;
    public readonly int ItemId;
    public readonly int Amount;

    public BeautyShopCost(ShopCurrencyType type, int amount) {
        Type = type;
        ItemId = 0;
        Amount = amount;
    }

    public BeautyShopCost(int itemId, int amount) {
        Type = ShopCurrencyType.Item;
        ItemId = itemId;
        Amount = amount;
    }

    public void WriteTo(IByteWriter writer) {
        writer.Write<ShopCurrencyType>(Type);
        writer.WriteInt(ItemId);
        writer.WriteInt(Amount);
        writer.WriteUnicodeString();
    }
}
