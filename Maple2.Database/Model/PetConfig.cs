using System;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class PetConfig {
    public long ItemUid { get; set; }

    public PetPotionConfig[] PotionConfigs { get; set; } = Array.Empty<PetPotionConfig>();
    public PetLootConfig LootConfig { get; set; } = new(true, true, true, true, true, true, true, false, 1, true);

    public static void Configure(EntityTypeBuilder<PetConfig> builder) {
        builder.ToTable("pet-config");
        builder.HasKey(config => config.ItemUid);
        builder.OneToOne<PetConfig, Item>()
            .HasForeignKey<PetConfig>(config => config.ItemUid);

        builder.Property(character => character.PotionConfigs).HasJsonConversion().IsRequired();
        builder.Property(character => character.LootConfig).HasJsonConversion().IsRequired();
    }
}
