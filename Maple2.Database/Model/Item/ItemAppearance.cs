using System.Numerics;
using System.Text.Json.Serialization;
using Maple2.Model.Common;

namespace Maple2.Database.Model;

internal abstract partial record ItemAppearance([JsonDiscriminator] ItemAppearance.Discriminator Type, EquipColor Color) {
    public enum Discriminator { Default = 1, Hair = 2, Decal = 3, Cap = 4 }
}

[JsonDiscriminatorFallback]
internal record ColorAppearance(EquipColor Color) : ItemAppearance(Discriminator.Default, Color) {
    public static implicit operator ColorAppearance?(Maple2.Model.Game.ItemAppearance? other) {
        return other == null ? null : new ColorAppearance(other.Color);
    }

    public static implicit operator Maple2.Model.Game.ItemAppearance?(ColorAppearance? other) {
        return other == null ? null : new Maple2.Model.Game.ItemAppearance(other.Color);
    }
}

internal record HairAppearance(EquipColor Color, float BackLength, Vector3 BackPosition1, Vector3 BackPosition2,
        float FrontLength, Vector3 FrontPosition1, Vector3 FrontPosition2) : ItemAppearance(Discriminator.Hair, Color) {
    public static implicit operator HairAppearance?(Maple2.Model.Game.HairAppearance? other) {
        return other == null ? null : new HairAppearance(other.Color, other.BackLength, other.BackPosition1,
            other.BackPosition2, other.FrontLength, other.FrontPosition1, other.FrontPosition2);
    }

    public static implicit operator Maple2.Model.Game.HairAppearance?(HairAppearance? other) {
        return other == null ? null : new Maple2.Model.Game.HairAppearance(other.Color, other.BackLength,
            other.BackPosition1, other.BackPosition2, other.FrontLength, other.FrontPosition1, other.FrontPosition2);
    }
}

internal record DecalAppearance(EquipColor Color, float Position1, float Position2, float Position3, float Position4)
        : ItemAppearance(Discriminator.Decal, Color) {
    public static implicit operator DecalAppearance?(Maple2.Model.Game.DecalAppearance? other) {
        return other == null ? null : new DecalAppearance(other.Color, other.Position1, other.Position2,
            other.Position3, other.Position4);
    }

    public static implicit operator Maple2.Model.Game.DecalAppearance?(DecalAppearance? other) {
        return other == null ? null : new Maple2.Model.Game.DecalAppearance(other.Color, other.Position1,
            other.Position2, other.Position3, other.Position4);
    }
}

internal record CapAppearance(EquipColor Color, Vector3 Position1, Vector3 Position2, Vector3 Position3,
        Vector3 Position4, float Unknown) : ItemAppearance(Discriminator.Cap, Color) {
    public static implicit operator CapAppearance?(Maple2.Model.Game.CapAppearance? other) {
        return other == null ? null : new CapAppearance(other.Color, other.Position1, other.Position2,
            other.Position3, other.Position4, other.Unknown);
    }

    public static implicit operator Maple2.Model.Game.CapAppearance?(CapAppearance? other) {
        return other == null ? null : new Maple2.Model.Game.CapAppearance(other.Color, other.Position1,
            other.Position2, other.Position3, other.Position4, other.Unknown);
    }
}
