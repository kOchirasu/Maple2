using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record FishingSpotTable(IReadOnlyDictionary<int, FishingSpotTable.Entry> Entries) : Table {
    public record Entry(int Id,
                        int MinMastery,
                        int MaxMastery,
                        IReadOnlyList<LiquidType> LiquidTypes);
}
