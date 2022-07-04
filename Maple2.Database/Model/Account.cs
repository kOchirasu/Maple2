using System;
using System.Collections.Generic;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
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
    public HomeInfo Home { get; set; }
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
            Home = other.Home,
            Trophy = other.Trophy,
            PremiumTime = other.PremiumTime,
            Currency = new AccountCurrency(),
        };
    }

    public static implicit operator Maple2.Model.Game.Account?(Account? other) {
        return other == null ? null : new Maple2.Model.Game.Account {
            LastModified = other.LastModified,
            Online = other.Online,
            Id = other.Id,
            Username = other.Username,
            MachineId = other.MachineId,
            MaxCharacters = other.MaxCharacters,
            PrestigeLevel = other.PrestigeLevel,
            PrestigeExp = other.PrestigeExp,
            Home = other.Home,
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
        builder.Property(account => account.Home).HasJsonConversion().IsRequired();

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

internal struct HomeInfo {
    public int MapId { get; set; }
    public int PlotId { get; set; }
    public int PlotMapId { get; set; }
    public int ApartmentNumber { get; set; }
    public PlotState State { get; set; }
    public int WeeklyArchitectScore { get; set; }
    public int ArchitectScore { get; set; }

    public byte Area { get; set; }
    public byte Height { get; set; }

    // Interior Settings
    public byte Background { get; set; }
    public byte Lighting { get; set; }
    public byte Camera { get; set; }

    public string? Name { get; set; }
    public string? Greeting { get; set; }
    public IDictionary<HomePermission, HomePermissionSetting> Permissions { get; set; }
    public DateTime UpdateTime { get; set; }
    public DateTime ExpiryTime { get; set; }

    public static implicit operator HomeInfo(Maple2.Model.Game.HomeInfo? other) {
        return other == null ? default : new HomeInfo {
            MapId = other.MapId,
            PlotId = other.PlotId,
            PlotMapId = other.PlotMapId,
            ApartmentNumber = other.ApartmentNumber,
            State = other.State,
            WeeklyArchitectScore = other.WeeklyArchitectScore,
            ArchitectScore = other.ArchitectScore,
            Area = other.Area,
            Height = other.Height,
            Background = other.Background,
            Lighting = other.Lighting,
            Camera = other.Camera,
            Name = other.Name,
            Greeting = other.Greeting,
            Permissions = other.Permissions,
            UpdateTime = other.UpdateTime.FromEpochSeconds(),
            ExpiryTime = other.ExpiryTime.FromEpochSeconds(),
        };
    }

    public static implicit operator Maple2.Model.Game.HomeInfo(HomeInfo other) {
        return new Maple2.Model.Game.HomeInfo(other.MapId, other.PlotId, other.PlotMapId, other.ApartmentNumber, other.State, other.WeeklyArchitectScore,
            other.ArchitectScore, other.Area, other.Height, other.Background, other.Lighting, other.Camera, other.Name, other.Greeting,
            other.ExpiryTime.ToEpochSeconds(), other.UpdateTime.ToEpochSeconds(), other.Permissions);
    }
}
