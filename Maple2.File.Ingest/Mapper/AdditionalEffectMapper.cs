using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.AdditionalEffect;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Tools.Extensions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
                    Status: Convert(data.StatusProperty, data.OffensiveProperty, data.DefensiveProperty),
                    Offensive: new AdditionalEffectMetadataOffensive(
                        ImmuneBreak: data.OffensiveProperty.hitImmuneBreak),
                    Defensive: new AdditionalEffectMetadataDefensive(
                        Invincible: data.DefensiveProperty.invincible != 0),
                    Recovery: Convert(data.RecoveryProperty),
                    Dot: new AdditionalEffectMetadataDot(
                        Damage: Convert(data.DotDamageProperty),
                        Buff: Convert(data.DotBuffProperty)),
                    Shield: Convert(data.ShieldProperty),
                    InvokeEffect: Convert(data.InvokeEffectProperty),
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

    private static void AddEntry<KeyType, Type>(Dictionary<KeyType, Type> dictionary, KeyType key, Type value) where Type : notnull where KeyType : notnull {
        if (!value.Equals(default)) {
            dictionary[key] = value;
        }
    }

    private static AdditionalEffectMetadataStatus Convert(StatusProperty status, OffensiveProperty offensive, DefensiveProperty defensive) {
        var values = new Dictionary<BasicAttribute, long>();
        var rates = new Dictionary<BasicAttribute, float>();
        var specialValues = new Dictionary<SpecialAttribute, float>();
        var specialRates = new Dictionary<SpecialAttribute, float>();
        
        if (status.Stat is not null) {
            foreach (BasicAttribute attribute in Enum.GetValues<BasicAttribute>()) {
                values.AddIfNotDefault(attribute, status.Stat.Value((byte)attribute));
                rates.AddIfNotDefault(attribute, status.Stat.Rate((byte)attribute));
            }
        }

        if (status.SpecialAbility is not null) {
            foreach (SpecialAttribute attribute in Enum.GetValues<SpecialAttribute>()) {
                byte attributeIndex = attribute.OptionIndex();

                if (attributeIndex != byte.MaxValue) {
                    specialValues.AddIfNotDefault(attribute, status.SpecialAbility.Value(attributeIndex));
                    specialRates.AddIfNotDefault(attribute, status.SpecialAbility.Rate(attributeIndex));
                }
            }
        }

        specialValues.AddIfNotDefault(SpecialAttribute.OffensiveMagicalDamage, offensive.mapDamageV);
        specialRates.AddIfNotDefault(SpecialAttribute.OffensiveMagicalDamage, offensive.mapDamageR);
        specialValues.AddIfNotDefault(SpecialAttribute.OffensivePhysicalDamage, offensive.papDamageV);
        specialRates.AddIfNotDefault(SpecialAttribute.OffensivePhysicalDamage, offensive.papDamageR);

        var resistances = new Dictionary<BasicAttribute, float>();

        resistances.AddIfNotDefault(BasicAttribute.MaxWeaponAtk, status.resWapR);
        resistances.AddIfNotDefault(BasicAttribute.BonusAtk, status.resBapR);
        resistances.AddIfNotDefault(BasicAttribute.CriticalDamage, status.resCadR);
        resistances.AddIfNotDefault(BasicAttribute.Accuracy, status.resAtpR);
        resistances.AddIfNotDefault(BasicAttribute.Evasion, status.resEvpR);
        resistances.AddIfNotDefault(BasicAttribute.Piercing, status.resPenR);
        resistances.AddIfNotDefault(BasicAttribute.AttackSpeed, status.resAspR);

        CompulsionEventType compulsionEventType = CompulsionEventType.None;

        Debug.Assert(status.compulsionEventTypes.Length < 2);
        Debug.Assert(status.compulsionEventRate.Length < 2);

        if (status.compulsionEventTypes.Length > 0) {
            compulsionEventType = (CompulsionEventType)status.compulsionEventTypes[0];
        }

        float compulsionEventRate = 0;

        if (status.compulsionEventRate.Length > 0) {
            compulsionEventRate = status.compulsionEventRate[0];
        }

        return new AdditionalEffectMetadataStatus(
            Values: values,
            Rates: rates,
            SpecialValues: specialValues,
            SpecialRates: specialRates,
            DeathResistanceHp: status.deathResistanceHP,
            Resistances: resistances,
            CompulsionEventType: compulsionEventType,
            CompulsionEventRate: compulsionEventRate,
            CompulsionEventSkillIds: status.compulsionEventSkillCodes);
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

    [return: NotNullIfNotNull(nameof(invokeEffect))]
    private static AdditionalEffectMetadataInvokeEffect? Convert (InvokeEffectProperty? invokeEffect) {
        if (invokeEffect is null) {
            return null;
        }

        return new AdditionalEffectMetadataInvokeEffect(
            Values: invokeEffect.values,
            Rates: invokeEffect.rates,
            Types: invokeEffect.types.Select(type => (InvokeEffectType)type).ToArray(),
            EffectId: invokeEffect.effectID,
            EffectGroupId: invokeEffect.effectGroupID,
            SkillId: invokeEffect.skillID,
            SkillGroupId: invokeEffect.skillGroupID);
    }
}
