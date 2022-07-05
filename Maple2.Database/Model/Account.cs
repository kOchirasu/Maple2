using System;
using System.Collections.Generic;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class Account {
    public long Id { get; set; }
    public string Username { get; set; }
    public bool Online { get; set; }
    public Guid MachineId { get; set; }
    public int MaxCharacters { get; set; }
    public int PrestigeLevel { get; set; }
    public long PrestigeExp { get; set; }
    public Trophy Trophy { get; set; }
    public long PremiumTime { get; set; }
    public AccountCurrency Currency { get; set; }

    public DateTime CreationTime { get; set; }
    public DateTime LastModified { get; set; }

    public ICollection<Character> Characters { get; set; }

    public static implicit operator Account?(Maple2.Model.Game.Account? other) {
        return other == null ? null : new Account {
            LastModified = other.LastModified,
            Id = other.Id,
            Username = other.Username,
            MachineId = other.MachineId,
            MaxCharacters = other.MaxCharacters,
            PrestigeLevel = other.PrestigeLevel,
            PrestigeExp = other.PrestigeExp,
            Trophy = other.Trophy,
            PremiumTime = other.PremiumTime,
            Currency = new AccountCurrency(),
        };
    }

    public static implicit operator Maple2.Model.Game.Account?(Account? other) {
        if (other == null) {
            return null;
        }

        return new Maple2.Model.Game.Account {
            LastModified = other.LastModified,
            Online = other.Online,
            Id = other.Id,
            Username = other.Username,
            MachineId = other.MachineId,
            MaxCharacters = other.MaxCharacters,
            PrestigeLevel = other.PrestigeLevel,
            PrestigeExp = other.PrestigeExp,
            Trophy = other.Trophy,
            PremiumTime = other.PremiumTime,
        };
    }

    public static void Configure(EntityTypeBuilder<Account> builder) {
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Username).IsRequired();
        builder.HasIndex(account => account.Username)
            .IsUnique();
        builder.Property(account => account.MaxCharacters)
            .HasDefaultValue(4);
        builder.HasMany(account => account.Characters);
        builder.Property(account => account.Trophy).HasJsonConversion().IsRequired();
        builder.Property(account => account.Currency).HasJsonConversion().IsRequired();

        builder.Property(account => account.LastModified).IsRowVersion();
        IMutableProperty creationTime = builder.Property(account => account.CreationTime)
            .ValueGeneratedOnAdd().Metadata;
        creationTime.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }
}

internal class AccountCurrency {
    public long Meret { get; set; }
    public long GameMeret { get; set; }
    public long MesoToken { get; set; }
}
