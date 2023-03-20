﻿using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record IndividualItemDropTable(
    Dictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> Entries): Table {

    public record Entry(
        int[] ItemIds,
        bool SmartGender,
        int SmartDropRate,
        int? Rarity,
        int EnchantLevel,
        bool ReduceTradeCount,
        bool ReduceRepackLimit,
        bool Bind,
        int MinCount,
        int MaxCount);
}
