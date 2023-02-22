using Maple2.Model.Enum;
using System;
using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model.Shop;

internal class ShopItem {
    public int Id { get; set; }
    public int ItemId { get; set; }
    public ShopCurrencyType CurrencyType { get; set; }
    public int CurrencyItemId { get; set; }
    public int Price { get; set; }
    public int SalePrice { get; set; }
    public byte Rarity { get; set; }
    public int StockCount { get; set; }
    public int StockPurchased { get; set; }
    public int RequireAchievementId { get; set; }
    public int RequireAchievementRank { get; set; }
    public byte RequireChampionshipGrade { get; set; }
    public short RequireChampionshipJoinCount { get; set; }
    public byte RequireGuildMerchantType { get; set; }
    public short RequireGuildMerchantLevel { get; set; }
    public short Quantity { get; set; }
    public ShopItemLabel Label { get; set; }
    public string CurrencyIdString { get; set; }
    public short RequireQuestAllianceId { get; set; }
    public int RequireFameGrade { get; set; }
    public bool AutoPreviewEquip { get; set; }

    /*[return: NotNullIfNotNull(nameof(other))]
    public static implicit operator FishEntry?(Maple2.Model.Game.FishEntry? other) {
        return other == null ? null : new FishEntry {
            Id = other.Id,
            TotalCaught = other.TotalCaught,
            TotalPrizeFish = other.TotalPrizeFish,
            LargestSize = other.LargestSize,
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.FishEntry?(FishEntry? other) {
        return other == null ? null : new Maple2.Model.Game.FishEntry(other.Id) {
            TotalCaught = other.TotalCaught,
            TotalPrizeFish = other.TotalPrizeFish,
            LargestSize = other.LargestSize,
        };
    }*/
    
    public static void Configure(EntityTypeBuilder<ShopItem> builder) {
        builder.ToTable("shop-item");
        builder.HasKey(shop => shop.Id);
    }
}
