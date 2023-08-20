using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record TrophyMetadata(
    int Id,
    string? Name,
    bool AccountWide,
    TrophyCategory Category,
    string[] CategoryTags,
    IReadOnlyDictionary<int, TrophyMetadataGrade> Grades) : ISearchResult;

public record TrophyMetadataGrade(
    TrophyMetadataCondition Condition,
    TrophyMetadataReward? Reward);

public record TrophyMetadataCondition(
    TrophyConditionType Type,
    TrophyMetadataCondition.Code? Codes,
    long Value,
    TrophyMetadataCondition.Code? Target) {


    public record Code(
        string[]? Strings,
        Range<int>? Range,
        int[]? Integers);
    public readonly record struct Range<T>(T Min, T Max) where T : INumber<T>;
}

public record TrophyMetadataReward(
    TrophyRewardType Type,
    int Code,
    int Value,
    int Rank);