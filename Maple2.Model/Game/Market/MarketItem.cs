using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public abstract class MarketItem : IByteSerializable {
    public readonly ItemMetadata ItemMetadata;
    public int ItemId { get; init; }
    protected string Name => ItemMetadata.Name ?? string.Empty;
    public long Price { get; init; }
    public int SalesCount { get; init; }
    public long CreationTime { get; init; }

    public MarketItem(ItemMetadata itemMetadata) {
        ItemMetadata = itemMetadata;
    }

    public virtual void WriteTo(IByteWriter writer) { }
}
