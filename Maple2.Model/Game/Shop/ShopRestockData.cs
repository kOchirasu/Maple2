using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game.Shop;

public class ShopRestockData : IByteSerializable {

    public ShopRestockInterval Interval { get; init; }
    public ShopCurrencyType CurrencyType { get; init; }
    public ShopCurrencyType ExcessCurrencyType { get; init; }
    public int Cost { get; init; }
    public bool EnableCostMultiplier { get; init; }
    public int RestockCount { get; set; }
    public bool DisableInstantRestock { get; init; }
    public bool PersistantInventory { get; init; }

    public void WriteTo(IByteWriter writer) {
        writer.Write<ShopCurrencyType>(CurrencyType);
        writer.Write<ShopCurrencyType>(ExcessCurrencyType);
        writer.WriteInt();
        writer.WriteInt(Cost);
        writer.WriteBool(EnableCostMultiplier);
        writer.WriteInt(RestockCount);
        writer.Write<ShopRestockInterval>(Interval);
        writer.WriteBool(DisableInstantRestock);
        writer.WriteBool(PersistantInventory);
    }
}
