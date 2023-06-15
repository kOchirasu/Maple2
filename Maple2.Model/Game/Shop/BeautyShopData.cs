using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Shop;

public class BeautyShopData : IByteSerializable {
    public BeautyShopCategory Category { get; init; }
    public readonly int Id;
    public BeautyShopType ShopType { get; init; }
    public int VoucherId { get; init; }
    public int ServiceRewardItemId { get; init; }
    public int ShopSubType { get; init; }
    public BeautyShopCost ServiceCost { get; init; } = BeautyShopCost.Zero;
    public BeautyShopCost RecolorCost { get; init; } = BeautyShopCost.Zero;

    protected BeautyShopData(int id) {
        Id = id;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.Write<BeautyShopCategory>(Category);
        writer.WriteInt(Id);
        writer.Write<BeautyShopType>(ShopType);
        writer.WriteInt(VoucherId);
        writer.WriteByte(); // Related to random hair tickets
        writer.WriteInt(ServiceRewardItemId);
        writer.WriteInt(ShopSubType);
        writer.WriteByte();
        writer.WriteClass<BeautyShopCost>(ServiceCost);
        writer.WriteClass<BeautyShopCost>(RecolorCost);
    }
}
