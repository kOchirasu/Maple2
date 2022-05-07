using System;
using System.Collections.Generic;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class CharacterConfig {
    public long CharacterId { get; set; }
    public IList<KeyBind> KeyBinds { get; set; }
    public IList<QuickSlot[]> HotBars { get; set; }

    public DateTime LastModified { get; set; }

    public static void Configure(EntityTypeBuilder<CharacterConfig> builder) {
        builder.ToTable("character-config");
        builder.HasKey(config => config.CharacterId);
        builder.OneToOne<CharacterConfig, Character>()
            .HasForeignKey<CharacterConfig>(config => config.CharacterId);
        builder.Property(config => config.KeyBinds).HasJsonConversion();
        builder.Property(config => config.HotBars).HasJsonConversion();

        builder.Property(unlock => unlock.LastModified).IsRowVersion();
    }
}
