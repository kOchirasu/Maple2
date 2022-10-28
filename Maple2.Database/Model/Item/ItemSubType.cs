using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maple2.Database.Model;

internal abstract partial record ItemSubType([JsonDiscriminator] ItemSubType.Discriminator Type) {
    public enum Discriminator { Ugc = 1, Pet = 2, Music = 3, Badge = 4 }
}

internal record ItemUgc(UgcItemLook Template, ItemBlueprint Blueprint) : ItemSubType(Discriminator.Ugc);

internal record UgcItemLook(string FileName, string Name, long AccountId, long CharacterId, string Author,
        long CreationTime, string Url) {
    [return:NotNullIfNotNull("other")]
    public static implicit operator UgcItemLook?(Maple2.Model.Game.UgcItemLook? other) {
        return other == null ? null : new UgcItemLook(other.FileName, other.Name, other.AccountId, other.CharacterId,
            other.Author, other.CreationTime, other.Url);
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.UgcItemLook?(UgcItemLook? other) {
        return other == null ? null : new Maple2.Model.Game.UgcItemLook {
            FileName = other.FileName,
            Name = other.Name,
            AccountId = other.AccountId,
            CharacterId = other.CharacterId,
            Author = other.Author,
            CreationTime = other.CreationTime,
            Url = other.Url,
        };
    }
}

internal record ItemBlueprint() {
    [return:NotNullIfNotNull("other")]
    public static implicit operator ItemBlueprint?(Maple2.Model.Game.ItemBlueprint? other) {
        return other == null ? null : new ItemBlueprint();
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.ItemBlueprint?(ItemBlueprint? other) {
        return other == null ? null : new Maple2.Model.Game.ItemBlueprint();
    }
}

internal record ItemPet(string Name, long Exp, int EvolvePoints, short Level, short RenameRemaining) : ItemSubType(Discriminator.Pet) {
    [return:NotNullIfNotNull("other")]
    public static implicit operator ItemPet?(Maple2.Model.Game.ItemPet? other) {
        return other == null ? null : new ItemPet(other.Name, other.Exp, other.EvolvePoints, other.Level, other.RenameRemaining);
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.ItemPet?(ItemPet? other) {
        return other == null ? null : new Maple2.Model.Game.ItemPet {
            Name = other.Name,
            Exp = other.Exp,
            EvolvePoints = other.EvolvePoints,
            Level = other.Level,
            RenameRemaining = other.RenameRemaining,
        };
    }
}

internal record ItemCustomMusicScore(int Length, int Instrument, string Title, string Author, long AuthorId,
        bool IsLocked, string Mml) : ItemSubType(Discriminator.Music) {
    [return:NotNullIfNotNull("other")]
    public static implicit operator ItemCustomMusicScore?(Maple2.Model.Game.ItemCustomMusicScore? other) {
        return other == null ? null : new ItemCustomMusicScore(other.Length, other.Instrument, other.Title,
            other.Author, other.AuthorId, other.IsLocked, other.Mml);
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.ItemCustomMusicScore?(ItemCustomMusicScore? other) {
        return other == null ? null : new Maple2.Model.Game.ItemCustomMusicScore {
            Length = other.Length,
            Instrument = other.Instrument,
            Title = other.Title,
            Author = other.Author,
            AuthorId = other.AuthorId,
            IsLocked = other.IsLocked,
            Mml = other.Mml,
        };
    }
}

internal record ItemBadge(int Id, bool[] Transparency, int PetSkinId) : ItemSubType(Discriminator.Badge) {
    [return:NotNullIfNotNull("other")]
    public static implicit operator ItemBadge?(Maple2.Model.Game.ItemBadge? other) {
        return other == null ? null : new ItemBadge(other.Id, other.Transparency, other.PetSkinId);
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.ItemBadge?(ItemBadge? other) {
        return other == null ? null : new Maple2.Model.Game.ItemBadge(other.Id, other.Transparency) {
            PetSkinId = other.PetSkinId,
        };
    }
}
