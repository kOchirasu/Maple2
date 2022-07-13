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
                    Recovery: new AdditionalEffectMetadataRecovery(
                        RecoveryRate: data.RecoveryProperty.RecoveryRate,
                        HpValue: data.RecoveryProperty.hpValue,
                        HpRate: data.RecoveryProperty.hpRate,
                        HpConsumeRate: data.RecoveryProperty.hpConsumeRate,
                        SpValue: data.RecoveryProperty.spValue,
                        SpRate: data.RecoveryProperty.spRate,
                        SpConsumeRate: data.RecoveryProperty.spConsumeRate,
                        EpValue: data.RecoveryProperty.epValue,
                        EpRate: data.RecoveryProperty.epRate,
                        NotCrit: data.RecoveryProperty.disableCriticalRecovery),
                    Dot: new AdditionalEffectMetadataDot(
                        Damage: data.DotDamageProperty.type > 0 ? new AdditionalEffectMetadataDot.DotDamage(
                            Type: data.DotDamageProperty.type,
                            Element: (Element) data.DotDamageProperty.element,
                            UseGrade: data.DotDamageProperty.useGrade,
                            Rate: data.DotDamageProperty.rate,
                            HpValue: data.DotDamageProperty.value,
                            SpValue: data.DotDamageProperty.spValue,
                            EpValue: data.DotDamageProperty.epValue,
                            DamageByTargetMaxHp: data.DotDamageProperty.damageByTargetMaxHP,
                            RecoverHpByDamage: data.DotDamageProperty.casterRecoveryHpByDamage,
                            IsConstDamage: data.DotDamageProperty.isConstDotDamageValue,
                            NotKill: data.DotDamageProperty.notKill) : null,
                        Buff: data.DotBuffProperty?.buffID > 0 ? new AdditionalEffectMetadataDot.DotBuff(
                            Target: (SkillEntity) data.DotBuffProperty.target,
                            Id: data.DotBuffProperty.buffID,
                            Level: data.DotBuffProperty.buffLevel) : null),
                    Shield: new AdditionalEffectMetadataShield(
                        HpValue: data.ShieldProperty.hpValue,
                        HpByTargetMaxHp: data.ShieldProperty.hpByTargetMaxHP),
                    Skills: data.conditionSkill.Concat(data.splashSkill).Select(skill => skill.Convert()).ToArray());
            }
        }
    }
}
