using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class CharacterConfig {
    public long CharacterId { get; set; }
    public IList<KeyBind>? KeyBinds { get; set; }
    public IList<QuickSlot[]>? HotBars { get; set; }
    public IList<SkillMacro>? SkillMacros { get; set; }
    public IList<Wardrobe>? Wardrobes { get; set; }
    public IDictionary<StatAttribute, int>? StatAllocation { get; set; }
    public SkillBook? SkillBook { get; set; }
    public ISet<int> FavoriteStickers { get; set; }
    public DateTime LastModified { get; set; }

    public static void Configure(EntityTypeBuilder<CharacterConfig> builder) {
        builder.ToTable("character-config");
        builder.HasKey(config => config.CharacterId);
        builder.OneToOne<CharacterConfig, Character>()
            .HasForeignKey<CharacterConfig>(config => config.CharacterId);
        builder.Property(config => config.KeyBinds).HasJsonConversion();
        builder.Property(config => config.HotBars).HasJsonConversion();
        builder.Property(config => config.SkillMacros).HasJsonConversion();
        builder.Property(config => config.Wardrobes).HasJsonConversion();
        builder.Property(config => config.StatAllocation).HasJsonConversion();

        builder.OwnsOne(config => config.SkillBook)
            .Property(skillBook => skillBook.MaxSkillTabs)
            .HasDefaultValue(1);
        builder.OwnsOne(config => config.SkillBook)
            .HasOne<SkillTab>()
            .WithOne()
            .HasPrincipalKey<SkillTab>(skillTab => skillTab.Id)
            .HasForeignKey<SkillBook>(skillBook => skillBook.ActiveSkillTabId);
        builder.Property(config => config.FavoriteStickers).HasJsonConversion();

        builder.Property(unlock => unlock.LastModified).IsRowVersion();
    }
}

internal class SkillMacro {
    public string Name { get; set; }
    public long KeyId { get; set; }
    public IList<int> Skills { get; set; }

    [return:NotNullIfNotNull("other")]
    public static implicit operator SkillMacro?(Maple2.Model.Game.SkillMacro? other) {
        return other == null ? new SkillMacro() : new SkillMacro {
            Name = other.Name,
            KeyId = other.KeyId,
            Skills = other.Skills.ToList(),
        };
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.SkillMacro?(SkillMacro? other) {
        return other == null ? new Maple2.Model.Game.SkillMacro(string.Empty, 0) :
            new Maple2.Model.Game.SkillMacro(other.Name, other.KeyId, other.Skills.ToHashSet());
    }
}

internal class Wardrobe {
    public int Type { get; set; }
    public int KeyId { get; set; }
    public string Name { get; set; }
    public Dictionary<EquipSlot, Equip> Equips { get; set; }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Wardrobe?(Maple2.Model.Game.Wardrobe? other) {
        return other == null ? new Wardrobe() : new Wardrobe {
            Type = other.Type,
            Name = other.Name,
            KeyId = other.KeyId,
            Equips = other.Equips.ToDictionary(
                entry => entry.Key,
                entry => new Equip {
                    ItemId = entry.Value.ItemId,
                    ItemUid = entry.Value.ItemUid,
                    Rarity = entry.Value.Rarity,
                }
            ),
        };
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.Wardrobe?(Wardrobe? other) {
        if (other == null) {
            return new Maple2.Model.Game.Wardrobe(0, string.Empty);
        }

        var wardrobe = new Maple2.Model.Game.Wardrobe(other.Type, other.Name) {
            KeyId = other.KeyId,
        };
        foreach ((EquipSlot slot, Equip equip) in other.Equips) {
            wardrobe.Equips[slot] = new Maple2.Model.Game.Wardrobe.Equip(equip.ItemUid, equip.ItemId, slot, equip.Rarity);
        }
        return wardrobe;
    }

    internal class Equip {
        public long ItemUid { get; set; }
        public int ItemId { get; set; }
        public int Rarity { get; set; }
    }
}

internal class SkillBook {
    public int MaxSkillTabs { get; set; }
    public long ActiveSkillTabId { get; set; }
}
