using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Tools.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class CharacterUnlock {
    public long CharacterId { get; set; }
    public ISet<int> Maps { get; set; }
    public ISet<int> Taxis { get; set; }
    public ISet<int> Titles { get; set; }
    public ISet<int> Emotes { get; set; }
    public IDictionary<int, long> StickerSets { get; set; }
    public IDictionary<int, short> Pets { get; set; }
    public InventoryExpand Expand { get; set; }
    public DateTime LastModified { get; init; }

    [return:NotNullIfNotNull("other")]
    public static implicit operator CharacterUnlock?(Maple2.Model.Game.Unlock? other) {
        return other == null ? new CharacterUnlock() : new CharacterUnlock {
            LastModified = other.LastModified,
            Expand = new InventoryExpand {
                Gear = other.Expand.GetValueOrDefault(InventoryType.Gear),
                Outfit = other.Expand.GetValueOrDefault(InventoryType.Outfit),
                Mount = other.Expand.GetValueOrDefault(InventoryType.Mount),
                Catalyst = other.Expand.GetValueOrDefault(InventoryType.Catalyst),
                FishingMusic = other.Expand.GetValueOrDefault(InventoryType.FishingMusic),
                Quest = other.Expand.GetValueOrDefault(InventoryType.Quest),
                Gemstone = other.Expand.GetValueOrDefault(InventoryType.Gemstone),
                Misc = other.Expand.GetValueOrDefault(InventoryType.Misc),
                LifeSkill = other.Expand.GetValueOrDefault(InventoryType.LifeSkill),
                Pets = other.Expand.GetValueOrDefault(InventoryType.Pets),
                Consumable = other.Expand.GetValueOrDefault(InventoryType.Consumable),
                Currency = other.Expand.GetValueOrDefault(InventoryType.Currency),
                Badge = other.Expand.GetValueOrDefault(InventoryType.Badge),
                Lapenshard = other.Expand.GetValueOrDefault(InventoryType.Lapenshard),
                Fragment = other.Expand.GetValueOrDefault(InventoryType.Fragment),
            },
            Maps = other.Maps,
            Taxis = other.Taxis,
            Titles = other.Titles,
            Emotes = other.Emotes,
            StickerSets = other.StickerSets,
            Pets = other.Pets,
        };
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.Unlock?(CharacterUnlock? other) {
        if (other == null) {
            return new Maple2.Model.Game.Unlock();
        }

        var unlock = new Maple2.Model.Game.Unlock {
            LastModified = other.LastModified,
            Expand = new Dictionary<InventoryType, short> {
                {InventoryType.Gear, other.Expand.Gear},
                {InventoryType.Outfit, other.Expand.Outfit},
                {InventoryType.Mount, other.Expand.Mount},
                {InventoryType.Catalyst, other.Expand.Catalyst},
                {InventoryType.FishingMusic, other.Expand.FishingMusic},
                {InventoryType.Quest, other.Expand.Quest},
                {InventoryType.Gemstone, other.Expand.Gemstone},
                {InventoryType.Misc, other.Expand.Misc},
                {InventoryType.LifeSkill, other.Expand.LifeSkill},
                {InventoryType.Pets, other.Expand.Pets},
                {InventoryType.Consumable, other.Expand.Consumable},
                {InventoryType.Currency, other.Expand.Currency},
                {InventoryType.Badge, other.Expand.Badge},
                {InventoryType.Lapenshard, other.Expand.Lapenshard},
                {InventoryType.Fragment, other.Expand.Fragment},
            }
        };

        unlock.Maps.UnionWith(other.Maps);
        unlock.Taxis.UnionWith(other.Taxis);
        unlock.Titles.UnionWith(other.Titles);
        unlock.Emotes.UnionWith(other.Emotes);

        foreach ((int petId, short rarity) in other.Pets) {
            if (unlock.Pets.TryGetValue(petId, out short existingRarity)) {
                unlock.Pets[petId] = Math.Max(existingRarity, rarity);
            } else {
                unlock.Pets[petId] = rarity;
            }
        }

        foreach ((int groupId, long expiration) in other.StickerSets) {
            unlock.StickerSets[groupId] = expiration;
        }

        return unlock;
    }

    public static void Configure(EntityTypeBuilder<CharacterUnlock> builder) {
        builder.ToTable("character-unlock");
        builder.HasKey(unlock => unlock.CharacterId);
        builder.OneToOne<CharacterUnlock, Character>()
            .HasForeignKey<CharacterUnlock>(unlock => unlock.CharacterId);
        builder.Property(unlock => unlock.Expand).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.Maps).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.Taxis).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.Titles).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.Emotes).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.StickerSets).HasJsonConversion();
        builder.Property(unlock => unlock.Pets).HasJsonConversion().IsRequired();

        builder.Property(unlock => unlock.LastModified).IsRowVersion();
    }
}

internal class InventoryExpand {
    public short Gear { get; set; }
    public short Outfit { get; set; }
    public short Mount { get; set; }
    public short Catalyst { get; set; }
    public short FishingMusic { get; set; }
    public short Quest { get; set; }
    public short Gemstone { get; set; }
    public short Misc { get; set; }
    public short LifeSkill { get; set; }
    public short Pets { get; set; }
    public short Consumable { get; set; }
    public short Currency { get; set; }
    public short Badge { get; set; }
    public short Lapenshard { get; set; }
    public short Fragment { get; set; }
}
