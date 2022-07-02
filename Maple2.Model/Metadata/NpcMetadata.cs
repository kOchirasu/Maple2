using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record NpcMetadata(
    int Id,
    string? Name,
    string Model,
    NpcMetadataStat Stat,
    NpcMetadataBasic Basic,
    NpcMetadataAction Action,
    NpcMetadataDead Dead) : ISearchResult;

public record NpcMetadataStat(
    IReadOnlyDictionary<StatAttribute, long> Stats,
    float[] ScaleStatRate,
    long[] ScaleBaseTap,
    long[] ScaleBaseDef,
    float[] ScaleBaseSpaRate);

public record NpcMetadataBasic(
    int Friendly,
    int AttackGroup,
    int DefenseGroup,
    int Kind,
    int ShopId,
    int HitImmune,
    int AbnormalImmune,
    short Level,
    int Class,
    bool RotationDisabled,
    int MaxSpawnCount,
    int GroupSpawnCount,
    int RareDegree,
    int Difficulty,
    long CustomExp);

public record NpcMetadataAction(
    float RotateSpeed,
    float WalkSpeed,
    float RunSpeed,
    NpcAction[] Actions,
    int MoveArea,
    string MaidExpired);

public record NpcAction(string Name, float Probability);

public record NpcMetadataDead(
    float Time,
    int Revival,
    int Count,
    float LifeTime,
    int ExtendRoomTime);
