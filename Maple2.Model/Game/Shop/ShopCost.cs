using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game.Shop;

public class ShopCost : IByteSerializable {
    public static ShopCost Zero = new(ShopCurrencyType.Meso, 0, 0);

    public readonly ShopCurrencyType Type;
    public readonly int ItemId;
    public readonly int Amount;
    public readonly int SaleAmount;

    public ShopCost(ShopCurrencyType type, int amount, int saleAmount) {
        Type = type;
        ItemId = 0;
        Amount = amount;
        SaleAmount = saleAmount;
    }

    public void WriteTo(IByteWriter writer) {
        writer.Write<ShopCurrencyType>(Type);
        writer.WriteInt(ItemId);
        writer.WriteInt();
        writer.WriteInt(Amount);
        writer.WriteInt(SaleAmount);
    }
}
