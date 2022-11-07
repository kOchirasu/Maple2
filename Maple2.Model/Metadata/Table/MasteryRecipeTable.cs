﻿using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record MasteryRecipeTable(IReadOnlyDictionary<int, MasteryRecipeTable.Entry> Entries) : Table(Discriminator.MasteryRecipeTable) {
    public record Entry(int Id,
                        MasteryType Type,
                        bool NoRewardExp,
                        int RequiredMastery,
                        long RequiredMeso,
                        IReadOnlyList<int> RequiredQuests,
                        long RewardExp,
                        int RewardMastery,
                        int HighPropLimitCount,
                        int NormalPropLimitCount,
                        IReadOnlyList<Ingredient> RequiredItems,
                        int HabitatMapId,
                        IReadOnlyList<Ingredient> RewardItems);

    public record Ingredient(int ItemId, int Rarity, int Amount);

}


