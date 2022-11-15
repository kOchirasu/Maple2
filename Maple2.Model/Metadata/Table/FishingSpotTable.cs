using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record FishingSpotTable(IReadOnlyDictionary<int, FishingSpotTable.Entry> Entries) : Table {
    public record Entry(int Id,
                        int MinMastery,
                        int MaxMastery,
                        IReadOnlyList<string> LiquidTypes);
}
