using System;
using System.Collections.Generic;
using Maple2.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class CharacterUnlock {
    public long CharacterId { get; set; }
    public ISet<int> Maps { get; set; }
    public ISet<int> Taxis { get; set; }
    public ISet<int> Titles { get; set; }
    public ISet<int> Emotes { get; set; }
    public ISet<int> Stamps { get; set; }
    public DateTime LastModified { get; init; }

    public static implicit operator CharacterUnlock(Maple2.Model.Game.Unlock other) {
        return other == null ? new CharacterUnlock() : new CharacterUnlock {
            LastModified = other.LastModified,
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

        var unlock = new Maple2.Model.Game.Unlock {
            LastModified = other.LastModified,
        };

        unlock.Maps.UnionWith(other.Maps);
        unlock.Taxis.UnionWith(other.Taxis);
        unlock.Titles.UnionWith(other.Titles);
        unlock.Emotes.UnionWith(other.Emotes);
        unlock.Stamps.UnionWith(other.Stamps);

        return unlock;
    }

    public static void Configure(EntityTypeBuilder<CharacterUnlock> builder) {
        builder.ToTable("character-unlock");
        builder.HasKey(unlock => unlock.CharacterId);
        builder.OneToOne<CharacterUnlock, Character>()
            .HasForeignKey<CharacterUnlock>(unlock => unlock.CharacterId);
        builder.Property(unlock => unlock.Maps).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.Taxis).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.Titles).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.Emotes).HasJsonConversion().IsRequired();
        builder.Property(unlock => unlock.Stamps).HasJsonConversion().IsRequired();

        builder.Property(unlock => unlock.LastModified).IsRowVersion();
    }
}
