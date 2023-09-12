using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class CharacterShopItemData {
    public int ShopId { get; set; }
    public int ShopItemId { get; set; }
    public long CharacterId { get; set; }
    public long AccountId { get; set; }
    public long ItemUid { get; set; }
    public int StockPurchased { get; set; }
    public bool IsPersistant { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Shop.CharacterShopItemData?(CharacterShopItemData? other) {
        return other == null ? null : new Maple2.Model.Game.Shop.CharacterShopItemData(other.ShopId, other.ShopItemId, other.ItemUid, other.CharacterId, other.AccountId, other.IsPersistant) {
            StockPurchased = other.StockPurchased,
        };
    }
    
    public static void Configure(EntityTypeBuilder<CharacterShopItemData> builder) {
        builder.ToTable("character-shop-item-data");
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
        builder.HasOne<Shop.ShopItem>()
            .WithMany()
            .HasForeignKey(shopItem => shopItem.ShopId);
        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(item => item.ShopId);
    }
}
