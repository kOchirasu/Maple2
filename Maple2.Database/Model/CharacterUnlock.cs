using System;
using System.Collections.Generic;
using Maple2.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class CharacterUnlock {
    public DateTime LastModified { get; set; }

    public long CharacterId { get; set; }

    public ISet<int> Maps { get; set; }
    public ISet<int> Taxis { get; set; }
    public ISet<int> Titles { get; set; }
    public ISet<int> Emotes { get; set; }
    public ISet<int> Stamps { get; set; }

    public static implicit operator CharacterUnlock(Maple2.Model.Game.Unlock other) {
        return other == null ? new CharacterUnlock() : new CharacterUnlock {
            Maps = other.Maps,
            Taxis = other.Taxis,
            Titles = other.Titles,
            Emotes = other.Emotes,
            Stamps = other.Stamps,
        };
    }

    public static implicit operator Maple2.Model.Game.Unlock(CharacterUnlock other) {
        if (other == null) {
            return new Maple2.Model.Game.Unlock();
        }

        var unlock = new Maple2.Model.Game.Unlock();
        if (other.Maps != null) {
            unlock.Maps.UnionWith(other.Maps);
        }
        if (other.Taxis != null) {
            unlock.Taxis.UnionWith(other.Taxis);
        }
        if (other.Titles != null) {
            unlock.Titles.UnionWith(other.Titles);
        }
        if (other.Emotes != null) {
            unlock.Emotes.UnionWith(other.Emotes);
        }
        if (other.Stamps != null) {
            unlock.Stamps.UnionWith(other.Stamps);
        }
        return unlock;
    }

    public static void Configure(EntityTypeBuilder<CharacterUnlock> builder) {
        builder.ToTable("character-unlock");
        builder.Property(character => character.LastModified).IsRowVersion();
        builder.HasKey(unlock => unlock.CharacterId);
        builder.OneToOne<CharacterUnlock, Character>()
            .HasForeignKey<CharacterUnlock>(unlock => unlock.CharacterId);
        builder.Property(unlock => unlock.Maps).HasJsonConversion();
        builder.Property(unlock => unlock.Taxis).HasJsonConversion();
        builder.Property(unlock => unlock.Titles).HasJsonConversion();
        builder.Property(unlock => unlock.Emotes).HasJsonConversion();
        builder.Property(unlock => unlock.Stamps).HasJsonConversion();
    }
}
