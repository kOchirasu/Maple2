using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record SkillMetadata(
    int Id,
    string? Name,
    SkillMetadataProperty Property,
    SkillMetadataState State,
    SkillMetadataLevel[] Levels);

public record SkillMetadataProperty(
    SkillType Type,
    SkillSubType SubType,
    SkillStyle SkillStyle,
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

public record SkillMetadataLevel();
