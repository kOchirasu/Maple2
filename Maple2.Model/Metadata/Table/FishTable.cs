using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record FishTable(IReadOnlyDictionary<int, FishTable.Entry> Entries) : Table {
    public record Entry(
        int Id,
        LiquidType FluidHabitat,
        IReadOnlyList<int> HabitatMapIds,
        int Mastery,
        short Rarity,
        Range<int> SmallSize,
        Range<int> BigSize);

    public readonly record struct Range<T>(T Min, T Max) where T : INumber<T>;
}
