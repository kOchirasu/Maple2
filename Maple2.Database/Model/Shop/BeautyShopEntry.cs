using Maple2.Model.Enum;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model.Shop;

internal class BeautyShopEntry {
    public int ShopId { get; set; }
    public int ItemId { get; set; }
    public ShopItemLabel Label { get; set; }
    public short RequireLevel { get; set; }
    public int RequireAchievementId { get; set; }
    public byte RequireAchievementRank { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Shop.BeautyShopEntry?(BeautyShopEntry? other) {
        return other == null ? null : new Maple2.Model.Game.Shop.BeautyShopEntry(other.ItemId) {
            Label = other.Label,
            RequireLevel = other.RequireLevel,
            RequireAchievementId = other.RequireAchievementId,
            RequireAchievementRank = other.RequireAchievementRank,
        };
    }
    
    public static void Configure(EntityTypeBuilder<BeautyShopEntry> builder) {
        builder.ToTable("beauty-shop-entry");
        builder.HasKey(entry => new {entry.ShopId, entry.ItemId}); 
        builder.HasOne<BeautyShop>()
            .WithMany()
            .HasForeignKey(item => item.ShopId);
    }
}
