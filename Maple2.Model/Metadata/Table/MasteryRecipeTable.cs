using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record MasteryRecipeTable(IReadOnlyDictionary<int, MasteryRecipeTable.Entry> Entries) : Table {
    public record Entry(int Id,
                        MasteryType Type,
                        bool NoRewardExp,
                        int RequiredMastery,
                        long RequiredMeso,
                        IReadOnlyList<int> RequiredQuests,
                        long RewardExp,
                        int RewardMastery,
                        int HighRateLimitCount,
                        int NormalRateLimitCount,
                        IReadOnlyList<Ingredient> RequiredItems,
                        IReadOnlyList<int> HabitatMapId,
                        IReadOnlyList<Ingredient> RewardItems);

    public record Ingredient(int ItemId, short Rarity, int Amount, string Tag);

}


