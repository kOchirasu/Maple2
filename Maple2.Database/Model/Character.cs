using System;
using Maple2.Database.Extensions;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class Character {
    public DateTime CreationTime { get; set; }
    public DateTime LastModified { get; set; }

    public long AccountId { get; set; }
    public Account Account { get; set; }
    
    public long Id { get; set; }
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public Job Job { get; set; }
    public short Level { get; set; }
    public SkinColor SkinColor { get; set; }
    public int MapId { get; set; }
    public Experience Experience { get; set; }
    public Profile Profile { get; set; }

    public static implicit operator Character(Maple2.Model.Game.Character other) {
        return other == null ? null : new Character {
            AccountId = other.AccountId,
            Account = other.Account,
            Id = other.Id,
            Name = other.Name,
            Gender = other.Gender,
            Job = other.Job,
            Level = other.Level,
            SkinColor = other.SkinColor,
            MapId = other.MapId,
            Experience = new Experience {
                Exp = other.Exp,
                RestExp = other.RestExp,
                Mastery = other.Mastery,
            },
            Profile = new Profile {
                Motto = other.Motto,
                Picture = other.Picture,
                Title = other.Title,
                Insignia = other.Insignia,
            },
        };
    }

    public static implicit operator Maple2.Model.Game.Character(Character other) {
        return other == null ? null : new Maple2.Model.Game.Character {
            AccountId = other.AccountId,
            Account = other.Account,
            Id = other.Id,
            Name = other.Name,
            CreationTime = other.CreationTime.ToEpochSeconds(),
            LastModified = other.LastModified.ToEpochSeconds(),
            Gender = other.Gender,
            Job = other.Job,
            Level = other.Level,
            SkinColor = other.SkinColor,
            Exp = other.Experience.Exp,
            RestExp = other.Experience.RestExp,
            MapId = other.MapId,
            Motto = other.Profile.Motto,
            Picture = other.Profile.Picture,
            Title = other.Profile.Title,
            Insignia = other.Profile.Insignia,
            Mastery = other.Experience.Mastery,
        };
    }

    public static void Configure(EntityTypeBuilder<Character> builder) {
        builder.Property(character => character.LastModified).IsRowVersion();
        builder.HasKey(character => character.Id);
        builder.HasOne<Account>(character => character.Account)
            .WithMany(account => account.Characters)
            .HasForeignKey(character => character.AccountId);
        builder.HasIndex(character => character.Name).IsUnique();
        builder.Property(character => character.CreationTime)
            .ValueGeneratedOnAdd();
        builder.Property(character => character.SkinColor).HasJsonConversion().IsRequired();
        builder.Property(character => character.Experience).HasJsonConversion().IsRequired();
        builder.Property(character => character.Profile).HasJsonConversion().IsRequired();
    }
}

internal class Experience {
    public long Exp { get; set; }
    public long RestExp { get; set; }
    public Mastery Mastery { get; set; }
}

internal class Profile {
    public string Motto { get; set; }
    public string Picture { get; set; }
    public int Title { get; set; }
    public short Insignia { get; set; }
}
