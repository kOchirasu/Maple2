using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record TrophyMetadata(
    int Id,
    string? Name,
    bool AccountWide,
    int NoticePercent,
    TrophyCategory Category,
    string[] CategoryTags,
    TrophyConditionType ConditionType,
    IReadOnlyDictionary<int, TrophyMetadataGrade> Grades) : ISearchResult;

public record TrophyMetadataGrade(
    TrophyMetadataCondition Condition,
    TrophyMetadataReward Reward);

public record TrophyMetadataCondition(
    string[] Code,
    long Value,
    string[] Target);

public record TrophyMetadataReward(
    TrophyRewardType Type,
    int Code,
    int Value,
    int Rank);