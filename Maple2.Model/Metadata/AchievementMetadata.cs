using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record AchievementMetadata(
    int Id,
    string? Name,
    bool AccountWide,
    AchievementCategory Category,
    string[] CategoryTags,
    IReadOnlyDictionary<int, AchievementMetadataGrade> Grades) : ISearchResult;

public record AchievementMetadataGrade(
    int Grade,
    AchievementMetadataCondition Condition,
    AchievementMetadataReward? Reward);

public record AchievementMetadataCondition(
    ConditionType Type,
    AchievementMetadataCondition.Parameters? Codes,
    long Value,
    AchievementMetadataCondition.Parameters? Target) {

    public record Parameters(
        string[]? Strings = null,
        Range<int>? Range = null,
        int[]? Integers = null);

    public readonly record struct Range<T>(T Min, T Max) where T : INumber<T>;
}

public record AchievementMetadataReward(
    AchievementRewardType Type,
    int Code,
    int Value,
    int Rank);
