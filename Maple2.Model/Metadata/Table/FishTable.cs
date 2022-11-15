using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record FishTable(IReadOnlyDictionary<int, FishTable.Entry> Entries) : Table {
    public record Entry(int Id,
                        string FluidHabitat,
                        IReadOnlyList<int> HabitatMapIds,
                        int Mastery,
                        short Rarity,
                        int[] SmallSize,
                        int[] BigSize);
}
