using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record MasteryRewardTable(IReadOnlyDictionary<MasteryType, MasteryRewardTable.Entry> Entries) : Table {
    public record Entry(MasteryType Type,
                        IReadOnlyDictionary<int, Level> Levels);

    public record Level(int Value, int ItemId, int ItemRarity, int ItemAmount);
}
