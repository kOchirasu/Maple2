using System;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class UgcMarketItem : MarketItem {
    public required long Id { get; init; }
    public required UgcMarketListingStatus Status { get; init; }
    public required long ListingEndTime { get; init; }
    public required long PromotionEndTime { get; init; }
    public required long SellerAccountId { get; init; }
    public required long SellerCharacterId { get; init; }
    public required string SellerCharacterName { get; init; }
    public required string Description { get; init; }
    public required string[] Tags { get; init; } = Array.Empty<string>();
    public required UgcItemLook Look { get; init; }

    public UgcMarketItem(ItemMetadata metadata) : base(metadata) { }

    public new void WriteTo(IByteWriter writer) {
        writer.WriteInt();
        writer.WriteLong(Id);
        writer.Write<UgcMarketListingStatus>(Status);
        writer.WriteInt(ItemMetadata.Id);
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
