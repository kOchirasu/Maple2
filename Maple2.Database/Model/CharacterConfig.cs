﻿using System;
using System.Collections.Generic;
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
    public IDictionary<BasicAttribute, int>? StatAllocation { get; set; }
    public SkillBook? SkillBook { get; set; }
    public IList<int>? FavoriteStickers { get; set; }
    public IDictionary<LapenshardSlot, int>? Lapenshards { get; set; }
    public IDictionary<GameEventUserValueType, GameEventUserValue>? GameEventValues { get; set; }

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
        builder.Property(config => config.Lapenshards).HasJsonConversion();

        builder.Property(unlock => unlock.LastModified).IsRowVersion();
    }
}

internal class SkillMacro {
    public required string Name { get; set; }
    public long KeyId { get; set; }
    public required IList<int> Skills { get; set; }

    public static implicit operator SkillMacro(Maple2.Model.Game.SkillMacro? other) {
        return other == null ? new SkillMacro {
            Name = string.Empty,
            Skills = Array.Empty<int>(),
        } : new SkillMacro {
            Name = other.Name,
            KeyId = other.KeyId,
            Skills = other.Skills.ToList(),
        };
    }

    public static implicit operator Maple2.Model.Game.SkillMacro(SkillMacro? other) {
        return other == null ? new Maple2.Model.Game.SkillMacro(string.Empty, 0) :
            new Maple2.Model.Game.SkillMacro(other.Name, other.KeyId, other.Skills.ToHashSet());
    }
}

internal class Wardrobe {
    public int Type { get; set; }
    public int KeyId { get; set; }
    public required string Name { get; set; }
    public required Dictionary<EquipSlot, Equip> Equips { get; set; }

    public static implicit operator Wardrobe(Maple2.Model.Game.Wardrobe? other) {
        return other == null ? new Wardrobe {
            Name = string.Empty,
            Equips = new Dictionary<EquipSlot, Equip>(),
        } : new Wardrobe {
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

    public static implicit operator Maple2.Model.Game.Wardrobe(Wardrobe? other) {
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

internal class GameEventUserValue {
    public long CharacterId { get; set; }
    public GameEventUserValueType Type { get; set; }
    public string Value { get; set; }
    public int EventId { get; set; } // TODO: Check if we really need to store this. Technically only one event can be active at a time.
    public long ExpirationTime { get; set; }
    
    public static implicit operator GameEventUserValue?(Maple2.Model.Game.GameEventUserValue? other) {
        return other == null ? null : new GameEventUserValue {
            Type = other.Type,
            Value = other.Value,
            EventId = other.EventId,
            ExpirationTime = other.ExpirationTime,
        };
    }

    public static implicit operator Maple2.Model.Game.GameEventUserValue?(GameEventUserValue? other) {
        return other == null ? null : new Maple2.Model.Game.GameEventUserValue {
            Type = other.Type,
            Value = other.Value,
            EventId = other.EventId,
            ExpirationTime = other.ExpirationTime,
        };
    }
    
    public static void Configure(EntityTypeBuilder<GameEventUserValue> builder) {
        builder.ToTable("game-event-user-value");
        builder.HasKey(config => config.CharacterId);
    }
}
