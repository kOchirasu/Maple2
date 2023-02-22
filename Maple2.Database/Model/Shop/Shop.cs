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
    
    public static void Configure(EntityTypeBuilder<Shop> builder) {
        builder.ToTable("shop");
        builder.HasKey(shop => shop.Id);
    }
}
