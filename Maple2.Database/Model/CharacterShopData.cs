using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class CharacterShopData {
    public int ShopId;
    public long CharacterId;
    public long AccountId;
    public long RestockTime;
    public int TotalRestockCount;

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Shop.Shop?(CharacterShopData? other) {
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
    
    public static void Configure(EntityTypeBuilder<CharacterShopData> builder) {
        builder.ToTable("character-shop-data");
        builder.HasKey(info => new {info.ShopId, info.CharacterId, info.AccountId});
        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(character => character.CharacterId);
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(account => account.AccountId);
        builder.HasOne<Shop.Shop>()
            .WithMany()
            .HasForeignKey(shop => shop.ShopId);
    }
}
