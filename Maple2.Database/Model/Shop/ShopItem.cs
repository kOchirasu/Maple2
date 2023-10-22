using System.Collections.Generic;
using Maple2.Model.Enum;
using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Extensions;
using Maple2.Model.Game.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model.Shop;

internal class ShopItem {
    public int Id { get; set; }
    public int ShopId { get; set; }
    public int ItemId { get; set; }
    public ShopCurrencyType CurrencyType { get; set; }
    public int CurrencyItemId { get; set; }
    public int Price { get; set; }
    public int SalePrice { get; set; }
    public byte Rarity { get; set; }
    public int StockCount { get; set; }
    public string Category { get; set; }
    public int RequireGuildTrophy { get; set; }
    public int RequireAchievementId { get; set; }
    public int RequireAchievementRank { get; set; }
    public byte RequireChampionshipGrade { get; set; }
    public short RequireChampionshipJoinCount { get; set; }
    public byte RequireGuildMerchantType { get; set; }
    public short RequireGuildMerchantLevel { get; set; }
    public short Quantity { get; set; }
    public ShopItemLabel Label { get; set; }
    public string IconCode { get; set; }
    public short RequireQuestAllianceId { get; set; }
    public int RequireFameGrade { get; set; }
    public bool AutoPreviewEquip { get; set; }
    public RestrictedBuyData? RestrictedBuyData { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Shop.ShopItem?(ShopItem? other) {
        return other == null ? null : new Maple2.Model.Game.Shop.ShopItem(other.Id) {
            ItemId = other.ItemId,
            Cost = new ShopCost {
                Type = other.CurrencyType,
                ItemId = other.CurrencyItemId,
                Amount = other.Price,
                SaleAmount = other.SalePrice,
            },
            Rarity = other.Rarity,
            StockCount = other.StockCount,
            RequireAchievementId = other.RequireAchievementId,
            RequireAchievementRank = other.RequireAchievementRank,
            RequireChampionshipGrade = other.RequireChampionshipGrade,
            RequireChampionshipJoinCount = other.RequireChampionshipJoinCount,
            RequireGuildMerchantType = other.RequireGuildMerchantType,
            RequireGuildMerchantLevel = other.RequireGuildMerchantLevel,
            RequireGuildTrophy = other.RequireGuildTrophy,
            Quantity = other.Quantity,
            Label = other.Label,
            IconCode = other.IconCode,
            RequireQuestAllianceId = other.RequireQuestAllianceId,
            RequireFameGrade = other.RequireFameGrade,
            AutoPreviewEquip = other.AutoPreviewEquip,
            Category = other.Category,
            RestrictedBuyData = other.RestrictedBuyData,
        };
    }

    public static void Configure(EntityTypeBuilder<ShopItem> builder) {
        builder.ToTable("shop-item");
        builder.HasKey(item => item.Id);
        builder.HasOne<Shop>()
            .WithMany()
            .HasForeignKey(item => item.ShopId);
        builder.Property(item => item.RestrictedBuyData).HasJsonConversion();
    }
}

internal class RestrictedBuyData {
    public long StartTime { get; init; }
    public long EndTime { get; init; }
    public IList<BuyTimeOfDay> TimeRanges { get; init; }
    public IList<ShopBuyDay> Days { get; init; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Shop.RestrictedBuyData?(RestrictedBuyData? other) {
        return other == null ? null : new Maple2.Model.Game.Shop.RestrictedBuyData {
            StartTime = other.StartTime,
            EndTime = other.EndTime,
            TimeRanges = other.TimeRanges,
            Days = other.Days,
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator RestrictedBuyData?(Maple2.Model.Game.Shop.RestrictedBuyData? other) {
        return other == null ? null : new RestrictedBuyData {
            StartTime = other.StartTime,
            EndTime = other.EndTime,
            TimeRanges = other.TimeRanges,
            Days = other.Days,
        };
    }

}
