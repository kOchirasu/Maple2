using System;
using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class PremiumMarketItem {
    public int Id { get; set; }
    public int ParentId { get; set; }
    public int TabId { get; set; }
    public int ItemId { get; set; }
    public byte Rarity { get; set; }
    public int Quantity { get; set; }
    public int BonusQuantity { get; set; }
    public int ItemDuration { get; set; }
    public MeretMarketCurrencyType CurrencyType { get; set; }
    public long Price { get; set; }
    public long SalePrice { get; set; }
    public DateTime SellBeginTime { get; set; }
    public DateTime SellEndTime { get; set; }
    public int SalesCount { get; set; }
    public MeretMarketItemLabel Label { get; set; }
    public JobFlag JobRequirement { get; set; }
    public int RequireAchievementId { get; set; }
    public int RequireAchievementRank { get; set; }
    public MeretMarketBannerLabel BannerLabel { get; set; }
    public required string BannerName { get; set; }
    public PremiumMarketPromoData? PromoData { get; set; }
    public bool RestockUnavailable { get; set; }
    public short RequireMinLevel { get; set; }
    public short RequireMaxLevel { get; set; }
    public bool PcCafe { get; set; }
    public bool ShowSaleTime { get; set; }
    public DateTime CreationTime { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator PremiumMarketItem?(Maple2.Model.Game.PremiumMarketItem? other) {
        return other == null ? null : new PremiumMarketItem {
            ParentId = other.ParentId,
            TabId = other.TabId,
            ItemId = other.ItemId,
            Rarity = other.Rarity,
            Quantity = other.Quantity,
            BonusQuantity = other.BonusQuantity,
            ItemDuration = other.ItemDuration,
            CurrencyType = other.CurrencyType,
            Price = other.Price,
            SalePrice = other.SalePrice,
            SellBeginTime = other.SellBeginTime.FromEpochSeconds(),
            SellEndTime = other.SellEndTime.FromEpochSeconds(),
            SalesCount = other.SalesCount,
            Label = other.Label,
            JobRequirement = other.JobRequirement,
            RequireAchievementId = other.RequireAchievementId,
            RequireAchievementRank = other.RequireAchievementRank,
            BannerName = other.BannerName,
            BannerLabel = other.BannerLabel,
            PromoData = other.PromoData,
            RestockUnavailable = other.RestockUnavailable,
            RequireMinLevel = other.RequireMinLevel,
            RequireMaxLevel = other.RequireMaxLevel,
            PcCafe = other.PcCafe,
            ShowSaleTime = other.ShowSaleTime,
            CreationTime = other.CreationTime.FromEpochSeconds(),
        };
    }

    public Maple2.Model.Game.PremiumMarketItem Convert(ItemMetadata metadata) {
        var entry = new Maple2.Model.Game.PremiumMarketItem(Id, metadata) {
            ParentId = ParentId,
            TabId = TabId,
            ItemId = ItemId,
            Rarity = Rarity,
            Quantity = Quantity,
            BonusQuantity = BonusQuantity,
            ItemDuration = ItemDuration,
            CurrencyType = CurrencyType,
            Price = Price,
            SalePrice = SalePrice,
            SellBeginTime = SellBeginTime.ToEpochSeconds(),
            SellEndTime = SellEndTime.ToEpochSeconds(),
            SalesCount = SalesCount,
            Label = Label,
            JobRequirement = JobRequirement,
            RequireAchievementId = RequireAchievementId,
            RequireAchievementRank = RequireAchievementRank,
            BannerName = BannerName,
            BannerLabel = BannerLabel,
            PromoData = PromoData,
            RestockUnavailable = RestockUnavailable,
            RequireMinLevel = RequireMinLevel,
            RequireMaxLevel = RequireMaxLevel,
            PcCafe = PcCafe,
            ShowSaleTime = ShowSaleTime,
            CreationTime = CreationTime.ToEpochSeconds(),
        };

        return entry;
    }

    public static void Configure(EntityTypeBuilder<PremiumMarketItem> builder) {
        builder.ToTable("premium-market-item");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.PromoData).HasJsonConversion();
        IMutableProperty creationTime = builder.Property(listing => listing.CreationTime)
            .ValueGeneratedOnAdd().Metadata;
        creationTime.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }
}

internal class PremiumMarketPromoData {
    public required string Name { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.PremiumMarketPromoData?(PremiumMarketPromoData? other) {
        return other == null ? null : new Maple2.Model.Game.PremiumMarketPromoData {
            Name = other.Name,
            StartTime = other.StartTime.ToEpochSeconds(),
            EndTime = other.EndTime.ToEpochSeconds(),
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator PremiumMarketPromoData?(Maple2.Model.Game.PremiumMarketPromoData? other) {
        return other == null ? null : new PremiumMarketPromoData() {
            Name = other.Name,
            StartTime = other.StartTime.FromEpochSeconds(),
            EndTime = other.EndTime.FromEpochSeconds(),
        };
    }
}
