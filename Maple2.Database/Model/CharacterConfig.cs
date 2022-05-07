using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class CharacterConfig {
    public long CharacterId { get; set; }
    public IList<KeyBind> KeyBinds { get; set; }
    public IList<QuickSlot[]> HotBars { get; set; }
    public IList<SkillMacro> SkillMacros { get; set; }

    public DateTime LastModified { get; set; }

    public static void Configure(EntityTypeBuilder<CharacterConfig> builder) {
        builder.ToTable("character-config");
        builder.HasKey(config => config.CharacterId);
        builder.OneToOne<CharacterConfig, Character>()
            .HasForeignKey<CharacterConfig>(config => config.CharacterId);
        builder.Property(config => config.KeyBinds).HasJsonConversion();
        builder.Property(config => config.HotBars).HasJsonConversion();
        builder.Property(config => config.SkillMacros).HasJsonConversion();

        builder.Property(unlock => unlock.LastModified).IsRowVersion();
    }
}

internal class SkillMacro {
    public string Name { get; set; }
    public long KeyId { get; set; }
    public IList<int> Skills { get; set; }

    public static implicit operator SkillMacro(Maple2.Model.Game.SkillMacro other) {
        return other == null ? new SkillMacro() : new SkillMacro {
            Name = other.Name,
            KeyId = other.KeyId,
            Skills = other.Skills.ToList(),
        };
    }

    public static implicit operator Maple2.Model.Game.SkillMacro(SkillMacro other) {
        return other == null ? new Maple2.Model.Game.SkillMacro(string.Empty, 0) :
            new Maple2.Model.Game.SkillMacro(other.Name, other.KeyId, other.Skills.ToHashSet());
    }
}
