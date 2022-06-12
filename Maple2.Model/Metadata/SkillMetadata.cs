using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

#region skill
public record SkillMetadata(
    int Id,
    string? Name,
    SkillMetadataProperty Property,
    SkillMetadataState State,
    Dictionary<short, SkillMetadataLevel> Levels);

public record SkillMetadataProperty(
    SkillType Type,
    SkillSubType SubType,
    RangeType RangeType,
    Element Element,
    bool ContinueSkill,
    bool SpRecoverySkill,
    bool ImmediateActive,
    //bool WeaponDependency, // 1,10300179,10300184
    bool UnrideOnHit,
    bool UnrideOnUse,
    bool ReleaseObjectWeapon,
    //bool DisableWater, // 10500061,10500065,10500190 (Sharp Eyes)
    //bool HoldAttack, // 10700171,10700252
    int SkillGroup);

public record SkillMetadataState();

public record SkillMetadataLevel(
    SkillMetadataConsume Consume,
    SkillMetadataRecovery Recovery,
    List<SkillMetadataSkill> Skills,
    List<SkillMetadataMotion> Motions);

public record SkillMetadataConsume(
    long Meso,
    bool UseItem,
    float HpRate,
    Dictionary<StatAttribute, long> Stat);

public record SkillMetadataRecovery(
    long SpValue,
    float SpRate);

public record SkillMetadataSkill(
    bool Splash,
    bool RandomCast,
    SkillMetadataSkill.Skill[] Skills,
    SkillEntity Target,
    SkillEntity Owner,
    uint Delay,
    int RemoveDelay,
    int Interval,
    int FireCount,
    bool ImmediateActive,
    bool NonTargetActive) {

    public record Skill(int Id, short Level);
}

public record SkillMetadataMotion(
    List<SkillMetadataAttack> Attacks);

public record SkillMetadataAttack(
    string Point,
    int PointGroup,
    int TargetCount,
    long MagicPathId,
    long CubeMagicPathId,
    SkillMetadataPet? Pet,
    SkillMetadataRegion Range,
    SkillMetadataDamage Damage);

public record SkillMetadataPet(
    int TamingGroup,
    int TrapLevel,
    int TamingPoint,
    bool ForcedTaming);

public record SkillMetadataRegion(
    SkillRegion Type,
    float Distance,
    float Height,
    float Width,
    float EndWidth,
    float RotateZDegree,
    Vector3 RangeAdd,
    Vector3 RangeOffset,
    SkillEntity IncludeCaster, // 0,1,2
    SkillEntity ApplyTarget, // 0,1,2,3,5,6,7,8
    SkillEntity CastTarget); // 0,1,2,3,4,5,7

public record SkillMetadataDamage(
    int Count,
    float Rate,
    float HitSpeed,
    float HitDelay,
    bool IsConstDamage, // Use 'Value' to determine damage
    long Value,
    float DamageByTargetMaxHp);
#endregion skill

#region magicpath
public record MagicPathMetadata(
    long Id,
    List<MagicPathMetadataMove> Moves);

public record MagicPathMetadataMove(
    bool Align,
    int AlignHeight,
    bool Rotate,
    bool IgnoreAdjust,
    Vector3 Direction,
    Vector3 FireOffset,
    Vector3 FireFixed,
    float Velocity,
    float Distance,
    float RotateZDegree,
    float LifeTime,
    float DelayTime,
    float SpawnTime,
    float DestroyTime);
#endregion
