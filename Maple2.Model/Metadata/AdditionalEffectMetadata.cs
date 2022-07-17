using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record AdditionalEffectMetadata(
    int Id,
    short Level,
    AdditionalEffectMetadataProperty Property,
    AdditionalEffectMetadataConsume Consume,
    AdditionalEffectMetadataRecovery? Recovery,
    AdditionalEffectMetadataDot Dot,
    AdditionalEffectMetadataShield? Shield,
    SkillEffectMetadata[] Skills);

public record AdditionalEffectMetadataProperty(
    BuffType Type,           // 1,2,3
    BuffSubType SubType,     // 0,1,2,4,6,8,16,32,64,128,256,512,1024
    BuffCategory Category,   // 0,1,2,4,6,7,8,9,99,1007,2001
    BuffEventType EventType, // 0,1,2,3,4
    int Group,
    int DurationTick,
    int IntervalTick,
    int DelayTick,
    int MaxCount,
    int KeepCondition,
    int ResetCondition,
    int DotCondition);

public record AdditionalEffectMetadataConsume(
    float HpRate,
    float SpRate);

public record AdditionalEffectMetadataRecovery(
    float RecoveryRate,
    long HpValue,
    float HpRate,
    float HpConsumeRate,
    long SpValue,
    float SpRate,
    float SpConsumeRate,
    long EpValue,
    float EpRate,
    bool NotCrit);

public record AdditionalEffectMetadataDot(
    AdditionalEffectMetadataDot.DotDamage? Damage,
    AdditionalEffectMetadataDot.DotBuff? Buff
) {
    public record DotDamage(
        int Type,
        Element Element,
        bool UseGrade,
        float Rate,
        int HpValue,
        int SpValue,
        int EpValue,
        float DamageByTargetMaxHp,
        float RecoverHpByDamage,
        bool IsConstDamage, // Use 'Value' to determine damage
        bool NotKill);

    public record DotBuff(
        SkillEntity Target, // 0,1
        int Id,
        short Level);
}

public record AdditionalEffectMetadataShield(
    long HpValue,
    float HpByTargetMaxHp);
