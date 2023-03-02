﻿using Maple2.Model.Enum;
using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record AdditionalEffectMetadata(
    int Id,
    short Level,
    BeginCondition Condition,
    AdditionalEffectMetadataProperty Property,
    AdditionalEffectMetadataConsume Consume,
    AdditionalEffectMetadataUpdate Update,
    AdditionalEffectMetadataStatus Status,
    AdditionalEffectMetadataRecovery? Recovery,
    AdditionalEffectMetadataDot Dot,
    AdditionalEffectMetadataShield? Shield,
    AdditionalEffectMetadataInvokeEffect? InvokeEffect,
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

public record AdditionalEffectMetadataUpdate(
    AdditionalEffectMetadataUpdate.CancelEffect? Cancel,
    int[] ImmuneIds,
    int[] ImmuneCategories,
    int[] ResetCooldown,
    AdditionalEffectMetadataUpdate.ModifyDuration[] Duration
) {
    public record CancelEffect(bool CheckSameCaster, bool PassiveEffect, int[] Ids, int[] Categories);

    public record ModifyDuration(int Id, float Rate, float Value);
}

public record AdditionalEffectMetadataStatus(
    IReadOnlyDictionary<BasicAttribute, long> Values,
    IReadOnlyDictionary<BasicAttribute, float> Rates,
    IReadOnlyDictionary<SpecialAttribute, float> SpecialValues,
    IReadOnlyDictionary<SpecialAttribute, float> SpecialRates,
    IReadOnlyDictionary<BasicAttribute, float> Resistances,
    long DeathResistanceHp,
    AdditionalEffectMetadataStatus.CompulsionEvent? Compulsion,
    int ImmuneBreak,
    bool Invincible) {

    public record CompulsionEvent(CompulsionEventType Type, float Rate, int[] SkillIds);
}

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

public record AdditionalEffectMetadataInvokeEffect(
    InvokeEffectType[] Types,
    float[] Values,
    float[] Rates,
    int EffectId,
    int EffectGroupId,
    int SkillId,
    int SkillGroupId);
