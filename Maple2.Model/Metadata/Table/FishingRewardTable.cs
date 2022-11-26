using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record FishingRewardTable(IReadOnlyDictionary<int, FishingRewardTable.Entry> Entries) : Table {
    public record Entry(int Id,
                        FishingItemType Type,
                        int Amount,
                        int Rarity);
}
