using System.Diagnostics.CodeAnalysis;
using Maple2.Model.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
    public bool CanRestock { get; set; }
    public long RestockTime { get; set; }
    public ShopRestockInterval RestockInterval { get; set; }
    public ShopCurrencyType RestockCurrencyType { get; set; }
    public ShopCurrencyType ExcessRestockCurrencyType { get; set; }
    public int RestockCost { get; set; }
    public bool EnableRestockCostMultiplier { get; set; }
    public int TotalRestockCount { get; set; }
    public bool DisableInstantRestock { get; set; }
    public bool PersistantInventory { get; set; }

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
            CanRestock = other.CanRestock,
            RestockTime = other.RestockTime,
            RestockData = new Maple2.Model.Game.Shop.ShopRestockData {
                Interval = other.RestockInterval,
                CurrencyType = other.RestockCurrencyType,
                ExcessCurrencyType = other.ExcessRestockCurrencyType,
                RestockCost = other.RestockCost,
                EnableRestockCostMultiplier = other.EnableRestockCostMultiplier,
                TotalRestockCount = other.TotalRestockCount,
                DisableInstantRestock = other.DisableInstantRestock,
                PersistantInventory = other.PersistantInventory,
            },
            
        };
    }
    
    public static void Configure(EntityTypeBuilder<Shop> builder) {
        builder.ToTable("shop");
        builder.HasKey(shop => shop.Id);
    }
}
