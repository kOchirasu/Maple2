using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class MarketEntry : IByteSerializable {
    public readonly ItemMetadata ItemMetadata;
    public int ItemId { get; init; }
    public long Price { get; init; }
    public int SalesCount { get; init; }
    public long CreationTime { get; init; }

    public MarketEntry(int itemId, long price, int salesCount, long creationTime) {
        ItemId = itemId;
        Price = price;
        SalesCount = salesCount;
        CreationTime = creationTime;
    }

    public MarketEntry(ItemMetadata itemMetadata) {
        ItemMetadata = itemMetadata;
    }
    
    public virtual void WriteTo(IByteWriter writer) {
    }
}
