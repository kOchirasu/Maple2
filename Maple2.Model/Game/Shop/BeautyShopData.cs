using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Shop;

public class BeautyShopData : IByteSerializable {
    public byte EntryType { get; init; }
    public readonly int Id;
    public int ShopType { get; init; }
    public int VoucherId { get; init; }
    public int ShopSubType { get; init; }
    public BeautyShopCost ItemCost { get; init; } = BeautyShopCost.Zero;
    public BeautyShopCost RecolorCost { get; init; } = BeautyShopCost.Zero;

    public BeautyShopData(int id) {
        Id = id;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.WriteByte(EntryType);
        writer.WriteInt(Id);
        writer.WriteInt(ShopType);
        writer.WriteInt(VoucherId);
        writer.WriteByte(); // Related to random hair tickets
        writer.WriteInt(); // Related to random hair tickets
        writer.WriteInt(ShopSubType);
        writer.WriteByte();
        writer.WriteClass<BeautyShopCost>(ItemCost);
        writer.WriteClass<BeautyShopCost>(RecolorCost);
    }
}
