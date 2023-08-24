using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class PremiumMarketEntry : MarketEntry {
    public int Id { get; init; }
    public int ParentId { get; init; }
    public int TabId { get; init; }
    public MeretMarketItemLabel Label { get; init; }
    public MeretMarketCurrencyType CurrencyType { get; init; }
    public long SalePrice { get; init; }
    public long SellBeginTime { get; init; }
    public long SellEndTime { get; init; }
    public JobFlag JobRequirement { get; init; }
    public bool RestockUnavailable { get; init; }
    public short RequireMinLevel { get; init; }
    public short RequireMaxLevel { get; init; }
    public byte Rarity { get; init; }
    public int Quantity { get; init; }
    public int ItemDuration { get; init; } // in days
    public int BonusQuantity { get; init; }
    public MeretMarketPromoBannerLabel PromoBannerLabel { get; init; }
    public string BannerName { get; init; }
    public int RequireAchievementId { get; init; }
    public int RequireAchievementRank { get; init; }
    public bool PcCafe { get; init; }
    public PremiumMarketPromoData? PromoData { get; init; }
    public bool ShowSaleTime { get; init; }
    public IList<PremiumMarketEntry> AdditionalQuantities { get; init; }

    public PremiumMarketEntry(int id, ItemMetadata metadata) : base(metadata) {
        Id = id;
        AdditionalQuantities = new List<PremiumMarketEntry>();
    }
    
    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteByte();
        writer.WriteUnicodeString(ItemMetadata.Name ?? string.Empty); // Item Name
        writer.WriteBool(true); // needs to be true for home page to work
        writer.WriteInt(ParentId);
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteByte();
        writer.Write<MeretMarketItemLabel>(Label);
        writer.Write<MeretMarketCurrencyType>(CurrencyType);
        writer.WriteLong(Price);
        writer.WriteLong(SalePrice);
        writer.WriteByte();
        writer.WriteLong(SellBeginTime);
        writer.WriteLong(SellEndTime);
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteBool(RestockUnavailable);
        writer.WriteInt();
        writer.WriteByte();
        writer.WriteShort(RequireMinLevel);
        writer.WriteShort(RequireMaxLevel);
        writer.Write<JobFlag>(JobRequirement);
        writer.WriteInt(ItemId);
        writer.WriteByte(Rarity);
        writer.WriteInt(Quantity);
        writer.WriteInt(ItemDuration);
        writer.WriteInt(BonusQuantity);
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteByte();
        writer.Write<MeretMarketPromoBannerLabel>(PromoBannerLabel);
        writer.WriteString(BannerName);
        writer.WriteString();
        writer.WriteByte();
        writer.WriteByte();
        writer.WriteInt();
        writer.WriteByte();
        writer.WriteInt(RequireAchievementId);
        writer.WriteInt(RequireAchievementRank);
        writer.WriteInt();
        writer.WriteBool(PcCafe);
        writer.WriteByte();
        writer.WriteInt();
        
    }
}
