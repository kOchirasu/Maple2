using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class UgcMarketItem : MarketItem {
    public long Id { get; init; }
    public UgcMarketListingStatus Status { get; init; }
    public long ListingEndTime { get; init; }
    public long PromotionEndTime { get; init; }
    public long SellerAccountId { get; init; }
    public long SellerCharacterId { get; init; }
    public string SellerCharacterName { get; init; }
    public string Description { get; init; }
    public string[] Tags { get; init; } = Array.Empty<string>();
    public UgcItemLook Look { get; init; }

    public UgcMarketItem(long id, ItemMetadata metadata) : base(metadata) {
        Id = id;
    }

    public new void WriteTo(IByteWriter writer) {
        writer.WriteInt();
        writer.WriteLong(Id);
        writer.Write<UgcMarketListingStatus>(Status);
        writer.WriteInt(ItemId);
        writer.WriteInt();
        writer.WriteByte();
        writer.WriteInt();
        writer.WriteLong(Price);
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteInt(SalesCount);
        writer.WriteInt();
        writer.WriteLong(CreationTime);
        writer.WriteLong(CreationTime);
        writer.WriteLong(ListingEndTime);
        writer.WriteInt();
        writer.WriteLong(PromotionEndTime);
        writer.WriteLong();
        writer.WriteLong(CreationTime);
        writer.WriteInt();
        writer.WriteLong(SellerAccountId);
        writer.WriteLong(SellerCharacterId);
        writer.WriteUnicodeString();
        writer.WriteUnicodeString(SellerCharacterName);
        writer.WriteUnicodeString(string.Join(",", Tags + ", " + Look.Name));
        writer.WriteUnicodeString(Description);
        writer.WriteUnicodeString(SellerCharacterName);
        writer.WriteClass<UgcItemLook>(Look);
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteUnicodeString();
        writer.WriteUnicodeString();
        writer.WriteString();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteUnicodeString();
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteUnicodeString();
        writer.Write<UgcMarketHomeCategory>(UgcMarketHomeCategory.None); // change for dictionary lookup when implemented
    }
}
