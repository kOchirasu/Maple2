using System;
using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model.Shop;

internal class Shop {
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public ShopSkin Skin { get; set; }
    public bool HideUnuseable { get; set; }
    public bool HideStats { get; set; }
    public bool DisableBuyback { get; set; }
    public bool OpenWallet { get; set; }
    public bool DisplayNew { get; set; }
    public bool RandomizeOrder { get; set; }
    public DateTime RestockTime { get; set; }
    public ShopRestockData? RestockData { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Shop.Shop?(Shop? other) {
        return other == null ? null : new Maple2.Model.Game.Shop.Shop(other.Id) {
            CategoryId = other.CategoryId,
            Name = other.Name,
            Skin = other.Skin,
            HideUnuseable = other.HideUnuseable,
            HideStats = other.HideStats,
            DisableBuyback = other.DisableBuyback,
            OpenWallet = other.OpenWallet,
            DisplayNew = other.DisplayNew,
            RandomizeOrder = other.RandomizeOrder,
            RestockData = other.RestockData,
            RestockTime = other.RestockTime.ToEpochSeconds(),
        };
    }

    public static void Configure(EntityTypeBuilder<Shop> builder) {
        builder.ToTable("shop");
        builder.HasKey(shop => shop.Id);
        builder.Property(shop => shop.RestockData).HasJsonConversion();
    }
}

internal class ShopRestockData {
    public ShopRestockInterval Interval { get; set; }
    public ShopCurrencyType CurrencyType { get; set; }
    public ShopCurrencyType ExcessCurrencyType { get; set; }
    public int Cost { get; set; }
    public bool EnableCostMultiplier { get; set; }
    public int RestockCount { get; set; }
    public bool DisableInstantRestock { get; set; }
    public bool PersistantInventory { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator ShopRestockData?(Maple2.Model.Game.Shop.ShopRestockData? other) {
        return other == null ? null : new ShopRestockData {
            Interval = other.Interval,
            CurrencyType = other.CurrencyType,
            ExcessCurrencyType = other.ExcessCurrencyType,
            Cost = other.Cost,
            EnableCostMultiplier = other.EnableCostMultiplier,
            RestockCount = other.RestockCount,
            DisableInstantRestock = other.DisableInstantRestock,
            PersistantInventory = other.PersistantInventory,
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Shop.ShopRestockData?(ShopRestockData? other) {
        return other == null ? null : new Maple2.Model.Game.Shop.ShopRestockData {
            Interval = other.Interval,
            CurrencyType = other.CurrencyType,
            ExcessCurrencyType = other.ExcessCurrencyType,
            Cost = other.Cost,
            EnableCostMultiplier = other.EnableCostMultiplier,
            RestockCount = other.RestockCount,
            DisableInstantRestock = other.DisableInstantRestock,
            PersistantInventory = other.PersistantInventory,
        };
    }
}
