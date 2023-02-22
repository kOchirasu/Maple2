using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Shop;

public class Shop : IByteSerializable {
    public int Id;
    public int NpcId;
    public int CategoryId { get; init; }
    public string Name  { get; init; }
    public ShopSkin Skin  { get; init; }
    public bool HideUnuseable  { get; init; }
    public bool HideStats  { get; init; }
    public bool DisableBuyback  { get; init; }
    public bool OpenWallet  { get; init; }
    public bool DisplayNew  { get; init; }
    public bool RandomizeOrder  { get; init; }
    public bool CanRestock  { get; init; }
    public long RestockTime  { get; init; }
    public ShopRestockData RestockData { get; init; }

    public virtual void WriteTo(IByteWriter writer) {
        writer.WriteInt(NpcId);
        writer.WriteInt(Id);
        writer.WriteLong(RestockTime);
        writer.WriteInt();
        writer.WriteShort(); // item count
        writer.WriteInt(CategoryId);
        writer.WriteBool(OpenWallet);
        writer.WriteBool(DisableBuyback);
        writer.WriteBool(CanRestock);
        writer.WriteBool(RandomizeOrder);
        writer.Write<ShopSkin>(Skin);
        writer.Write(HideUnuseable);
        writer.WriteBool(HideStats);
        writer.WriteBool(false);
        writer.WriteBool(DisplayNew);
        writer.WriteUnicodeString(Name);
        if (CanRestock) {
            writer.WriteClass<ShopRestockData>(RestockData);
        }
    }
}
