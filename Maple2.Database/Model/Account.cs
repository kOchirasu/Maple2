using System;
using System.Collections.Generic;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class Account {
    public long Id { get; set; }
    public string Username { get; set; }
    public Guid MachineId { get; set; }
    public DateTime LastModified { get; set; }
    public long Merets { get; set; }
    public int MaxCharacters { get; set; }
    public int PrestigeLevel { get; set; }
    public long PrestigeExp { get; set; }
    public Trophy Trophy { get; set; }
    public long PremiumTime { get; set; }

    public ICollection<Character> Characters { get; set; }

    public static implicit operator Account(Maple2.Model.Game.Account other) {
        return other == null ? null : new Account {
            Id = other.Id,
            Username = other.Username,
            MachineId = other.MachineId,
            Merets = other.Merets,
            MaxCharacters = other.MaxCharacters,
            PrestigeLevel = other.PrestigeLevel,
            PrestigeExp = other.PrestigeExp,
            Trophy = other.Trophy,
            PremiumTime = other.PremiumTime,
        };
    }

    public static implicit operator Maple2.Model.Game.Account(Account other) {
        return other == null ? null : new Maple2.Model.Game.Account {
            Id = other.Id,
            Username = other.Username,
            MachineId = other.MachineId,
            Merets = other.Merets,
            MaxCharacters = other.MaxCharacters,
            PrestigeLevel = other.PrestigeLevel,
            PrestigeExp = other.PrestigeExp,
            Trophy = other.Trophy,
            PremiumTime = other.PremiumTime,
        };
    }

    public static void Configure(EntityTypeBuilder<Account> builder) {
        builder.Property(account => account.LastModified).IsRowVersion();
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Username).IsRequired();
        builder.HasIndex(account => account.Username)
            .IsUnique();
        builder.Property(account => account.MaxCharacters)
            .HasDefaultValue(4);
        builder.HasMany(account => account.Characters);
        builder.Property(account => account.Trophy).HasJsonConversion();
    }
}
