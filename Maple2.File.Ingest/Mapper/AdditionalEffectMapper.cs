using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.AdditionalEffect;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class AdditionalEffectMapper : TypeMapper<AdditionalEffectMetadata> {
    private readonly AdditionalEffectParser parser;

    public AdditionalEffectMapper(M2dReader xmlReader) {
        parser = new AdditionalEffectParser(xmlReader);
    }

    protected override IEnumerable<AdditionalEffectMetadata> Map() {
        foreach ((int id, IList<AdditionalEffectData> datas) in parser.Parse()) {
            foreach (AdditionalEffectData data in datas) {
                yield return new AdditionalEffectMetadata(
                    Id: id,
                    Level: data.BasicProperty.level,
                    Condition: data.beginCondition.Convert(),
                    Property: new AdditionalEffectMetadataProperty(
                        Type: (BuffType) data.BasicProperty.buffType,
                        SubType: (BuffSubType) data.BasicProperty.buffSubType,
                        Category: (BuffCategory) data.BasicProperty.buffCategory,
                        EventType: (BuffEventType) data.BasicProperty.eventBuffType,
                        Group: data.BasicProperty.group,
                        DurationTick: data.BasicProperty.durationTick,
                        IntervalTick: data.BasicProperty.intervalTick,
                        DelayTick: data.BasicProperty.delayTick,
                        MaxCount: data.BasicProperty.maxBuffCount,
                        KeepCondition: data.BasicProperty.keepCondition,
                        ResetCondition: data.BasicProperty.resetCondition,
                        DotCondition: data.BasicProperty.dotCondition),
                    Consume: new AdditionalEffectMetadataConsume(
                        HpRate: data.ConsumeProperty.hpRate,
                        SpRate: data.ConsumeProperty.spRate),
                    Update: Convert(data),
                    Recovery: Convert(data.RecoveryProperty),
                    Dot: new AdditionalEffectMetadataDot(
                        Damage: Convert(data.DotDamageProperty),
                        Buff: Convert(data.DotBuffProperty)),
                    Shield: Convert(data.ShieldProperty),
                    Skills: data.conditionSkill.Concat(data.splashSkill).Select(skill => skill.Convert()).ToArray());
            }
        }
    }

    private static AdditionalEffectMetadataUpdate Convert(AdditionalEffectData data) {
        CancelEffectProperty cancel = data.CancelEffectProperty;
        AdditionalEffectMetadataUpdate.CancelEffect? cancelEffect = null;
        if (cancel.cancelEffectCodes.Length != 0 || cancel.cancelBuffCategories.Length != 0) {
            cancelEffect = new AdditionalEffectMetadataUpdate.CancelEffect(
                CheckSameCaster: cancel.cancelCheckSameCaster,
                PassiveEffect: cancel.cancelPassiveEffect,
                Ids: cancel.cancelEffectCodes,
                Categories: cancel.cancelBuffCategories);
        }

        ModifyEffectDurationProperty modify = data.ModifyEffectDurationProperty;
        var modifyDuration = new AdditionalEffectMetadataUpdate.ModifyDuration[modify.effectCodes.Length];
        for (int i = 0; i < modifyDuration.Length; i++) {
            modifyDuration[i] = new AdditionalEffectMetadataUpdate.ModifyDuration(modify.effectCodes[i], modify.durationFactors[i], modify.durationValues[i]);
        }

        return new AdditionalEffectMetadataUpdate(
            Cancel: cancelEffect,
            ImmuneIds: data.ImmuneEffectProperty.immuneEffectCodes,
            ImmuneCategories: data.ImmuneEffectProperty.immuneBuffCategories,
            ResetCooldown: data.ResetSkillCoolDownTimeProperty.skillCodes,
            Duration: modifyDuration);
    }

    private static AdditionalEffectMetadataRecovery? Convert(RecoveryProperty recovery) {
        if (recovery.RecoveryRate <= 0 && recovery.hpValue <= 0 && recovery.hpRate <= 0 && recovery.spValue <= 0 && recovery.spRate <= 0
            && recovery.spConsumeRate <= 0 && recovery.epValue <= 0 && recovery.epRate <= 0) {
            return null;
        }

        return new AdditionalEffectMetadataRecovery(
            RecoveryRate: recovery.RecoveryRate,
            HpValue: recovery.hpValue,
            HpRate: recovery.hpRate,
            HpConsumeRate: recovery.hpConsumeRate,
            SpValue: recovery.spValue,
            SpRate: recovery.spRate,
            SpConsumeRate: recovery.spConsumeRate,
            EpValue: recovery.epValue,
            EpRate: recovery.epRate,
            NotCrit: recovery.disableCriticalRecovery);
    }

    private static AdditionalEffectMetadataDot.DotDamage? Convert(DotDamageProperty dotDamage) {
        if (dotDamage.type <= 0) {
            return null;
        }

        return new AdditionalEffectMetadataDot.DotDamage(
            Type: dotDamage.type,
            Element: (Element) dotDamage.element,
            UseGrade: dotDamage.useGrade,
            Rate: dotDamage.rate,
            HpValue: dotDamage.value,
            SpValue: dotDamage.spValue,
            EpValue: dotDamage.epValue,
            DamageByTargetMaxHp: dotDamage.damageByTargetMaxHP,
            RecoverHpByDamage: dotDamage.casterRecoveryHpByDamage,
            IsConstDamage: dotDamage.isConstDotDamageValue,
            NotKill: dotDamage.notKill);
    }

    private static AdditionalEffectMetadataDot.DotBuff? Convert(DotBuffProperty? dotBuff) {
        if (dotBuff is not {buffID: > 0}) {
            return null;
        }

        return new AdditionalEffectMetadataDot.DotBuff(Target: (SkillEntity) dotBuff.target, Id: dotBuff.buffID, Level: dotBuff.buffLevel);
    }

    private static AdditionalEffectMetadataShield? Convert(ShieldProperty shield) {
        if (shield.hpValue <= 0 && shield.hpByTargetMaxHP <= 0) {
            return null;
        }

        return new AdditionalEffectMetadataShield(HpValue: shield.hpValue, HpByTargetMaxHp: shield.hpByTargetMaxHP);
    }
}
