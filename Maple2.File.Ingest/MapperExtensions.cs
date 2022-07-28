using Maple2.File.Parser.Xml.Common;
using Maple2.File.Parser.Xml.Skill;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using BeginCondition = Maple2.Model.Metadata.BeginCondition;

namespace Maple2.File.Ingest;

public static class MapperExtensions {
    public static Dictionary<StatAttribute, long> ToDictionary(this StatValue values) {
        var result = new Dictionary<StatAttribute, long>();
        if (values.str != default) result[StatAttribute.Strength] = values.str;
        if (values.dex != default) result[StatAttribute.Dexterity] = values.dex;
        if (values.@int != default) result[StatAttribute.Intelligence] = values.@int;
        if (values.luk != default) result[StatAttribute.Luck] = values.luk;
        if (values.hp != default) result[StatAttribute.Health] = values.hp;
        if (values.hp_rgp != default) result[StatAttribute.HpRegen] = values.hp_rgp;
        if (values.hp_inv != default) result[StatAttribute.HpRegenInterval] = values.hp_inv;
        if (values.sp != default) result[StatAttribute.Spirit] = values.sp;
        if (values.sp_rgp != default) result[StatAttribute.SpRegen] = values.sp_rgp;
        if (values.sp_inv != default) result[StatAttribute.SpRegenInterval] = values.sp_inv;
        if (values.ep != default) result[StatAttribute.Stamina] = values.ep;
        if (values.ep_rgp != default) result[StatAttribute.StaminaRegen] = values.ep_rgp;
        if (values.ep_inv != default) result[StatAttribute.StaminaRegenInterval] = values.ep_inv;
        if (values.asp != default) result[StatAttribute.AttackSpeed] = values.asp;
        if (values.msp != default) result[StatAttribute.MovementSpeed] = values.msp;
        if (values.atp != default) result[StatAttribute.Accuracy] = values.atp;
        if (values.evp != default) result[StatAttribute.Evasion] = values.evp;
        if (values.cap != default) result[StatAttribute.CriticalRate] = values.cap;
        if (values.cad != default) result[StatAttribute.CriticalDamage] = values.cad;
        if (values.car != default) result[StatAttribute.CriticalEvasion] = values.car;
        if (values.ndd != default) result[StatAttribute.Defense] = values.ndd;
        if (values.abp != default) result[StatAttribute.PerfectGuard] = values.abp;
        if (values.jmp != default) result[StatAttribute.JumpHeight] = values.jmp;
        if (values.pap != default) result[StatAttribute.PhysicalAtk] = values.pap;
        if (values.map != default) result[StatAttribute.MagicalAtk] = values.map;
        if (values.par != default) result[StatAttribute.PhysicalRes] = values.par;
        if (values.mar != default) result[StatAttribute.MagicalRes] = values.mar;
        if (values.wapmin != default) result[StatAttribute.MinWeaponAtk] = values.wapmin;
        if (values.wapmax != default) result[StatAttribute.MaxWeaponAtk] = values.wapmax;
        if (values.dmg != default) result[StatAttribute.Damage] = values.dmg;
        if (values.pen != default) result[StatAttribute.Piercing] = values.pen;
        if (values.rmsp != default) result[StatAttribute.MountSpeed] = values.rmsp;
        if (values.bap != default) result[StatAttribute.BonusAtk] = values.bap;
        if (values.bap_pet != default) result[StatAttribute.PetBonusAtk] = values.bap_pet;
        return result;
    }

    public static Dictionary<StatAttribute, float> ToDictionary(this StatRate rates) {
        var result = new Dictionary<StatAttribute, float>();
        if (rates.str != default) result[StatAttribute.Strength] = rates.str;
        if (rates.dex != default) result[StatAttribute.Dexterity] = rates.dex;
        if (rates.@int != default) result[StatAttribute.Intelligence] = rates.@int;
        if (rates.luk != default) result[StatAttribute.Luck] = rates.luk;
        if (rates.hp != default) result[StatAttribute.Health] = rates.hp;
        if (rates.hp_rgp != default) result[StatAttribute.HpRegen] = rates.hp_rgp;
        if (rates.hp_inv != default) result[StatAttribute.HpRegenInterval] = rates.hp_inv;
        if (rates.sp != default) result[StatAttribute.Spirit] = rates.sp;
        if (rates.sp_rgp != default) result[StatAttribute.SpRegen] = rates.sp_rgp;
        if (rates.sp_inv != default) result[StatAttribute.SpRegenInterval] = rates.sp_inv;
        if (rates.ep != default) result[StatAttribute.Stamina] = rates.ep;
        if (rates.ep_rgp != default) result[StatAttribute.StaminaRegen] = rates.ep_rgp;
        if (rates.ep_inv != default) result[StatAttribute.StaminaRegenInterval] = rates.ep_inv;
        if (rates.asp != default) result[StatAttribute.AttackSpeed] = rates.asp;
        if (rates.msp != default) result[StatAttribute.MovementSpeed] = rates.msp;
        if (rates.atp != default) result[StatAttribute.Accuracy] = rates.atp;
        if (rates.evp != default) result[StatAttribute.Evasion] = rates.evp;
        if (rates.cap != default) result[StatAttribute.CriticalRate] = rates.cap;
        if (rates.cad != default) result[StatAttribute.CriticalDamage] = rates.cad;
        if (rates.car != default) result[StatAttribute.CriticalEvasion] = rates.car;
        if (rates.ndd != default) result[StatAttribute.Defense] = rates.ndd;
        if (rates.abp != default) result[StatAttribute.PerfectGuard] = rates.abp;
        if (rates.jmp != default) result[StatAttribute.JumpHeight] = rates.jmp;
        if (rates.pap != default) result[StatAttribute.PhysicalAtk] = rates.pap;
        if (rates.map != default) result[StatAttribute.MagicalAtk] = rates.map;
        if (rates.par != default) result[StatAttribute.PhysicalRes] = rates.par;
        if (rates.mar != default) result[StatAttribute.MagicalRes] = rates.mar;
        if (rates.wapmin != default) result[StatAttribute.MinWeaponAtk] = rates.wapmin;
        if (rates.wapmax != default) result[StatAttribute.MaxWeaponAtk] = rates.wapmax;
        if (rates.dmg != default) result[StatAttribute.Damage] = rates.dmg;
        if (rates.pen != default) result[StatAttribute.Piercing] = rates.pen;
        if (rates.rmsp != default) result[StatAttribute.MountSpeed] = rates.rmsp;
        if (rates.bap != default) result[StatAttribute.BonusAtk] = rates.bap;
        if (rates.bap_pet != default) result[StatAttribute.PetBonusAtk] = rates.bap_pet;
        return result;
    }

    public static SkillEffectMetadata Convert(this TriggerSkill trigger) {
        SkillEffectMetadataCondition? condition = null;
        SkillEffectMetadataSplash? splash = null;
        if (trigger.splash) {
            splash = new SkillEffectMetadataSplash(
                Interval: trigger.interval,
                Delay: trigger.delay > int.MaxValue ? int.MaxValue : (int) trigger.delay,
                RemoveDelay: trigger.removeDelay,
                UseDirection: trigger.useDirection,
                ImmediateActive: trigger.immediateActive,
                NonTargetActive: trigger.nonTargetActive,
                OnlySensingActive: trigger.onlySensingActive,
                DependOnCasterState: trigger.dependOnCasterState,
                Independent: trigger.independent,
                Chain: trigger.chain ? new SkillEffectMetadataChain(trigger.chainDistance) : null);
        } else {
            var owner = SkillEntity.Target;
            if (trigger.skillOwner > 0 && Enum.IsDefined<SkillEntity>((SkillEntity) trigger.skillOwner)) {
                owner = (SkillEntity) trigger.skillOwner;
            }
            condition = new SkillEffectMetadataCondition(
                Condition: trigger.beginCondition.Convert(),
                Owner: owner,
                Target: (SkillEntity) trigger.skillTarget,
                OverlapCount: trigger.overlapCount,
                RandomCast: trigger.randomCast,
                ActiveByIntervalTick: trigger.activeByIntervalTick,
                DependOnDamageCount: trigger.dependOnDamageCount);
        }

        SkillEffectMetadata.Skill[] skills;
        if (trigger.linkSkillID.Length > 0) {
            skills = trigger.skillID
                .Zip(trigger.level, (skillId, level) => new {skillId, level})
                .Zip(trigger.linkSkillID, (skill, linkSkillId) => new SkillEffectMetadata.Skill(skill.skillId, skill.level, linkSkillId))
                .ToArray();
        } else {
            skills = trigger.skillID
                .Zip(trigger.level, (skillId, level) => new SkillEffectMetadata.Skill(skillId, level))
                .ToArray();
        }

        return new SkillEffectMetadata(
            FireCount: trigger.fireCount,
            Skills: skills,
            Condition: condition,
            Splash: splash);
    }

    public static BeginCondition Convert(this Maple2.File.Parser.Xml.Skill.BeginCondition beginCondition) {
        return new BeginCondition(
            Level: beginCondition.level,
            Gender: (Gender) beginCondition.gender,
            Mesos: beginCondition.money,
            JobCode: beginCondition.job.Select(job => (JobCode) job.code).ToArray(),
            Target: Convert(beginCondition.skillTarget),
            Owner: Convert(beginCondition.skillOwner),
            Caster: Convert(beginCondition.skillCaster));
    }

    // We use this default to avoid writing useless checks
    private static readonly BeginConditionTarget DefaultBeginConditionTarget = new(Array.Empty<BeginConditionTarget.HasBuff>(), null, null);
    private static BeginConditionTarget? Convert(SubConditionTarget? target) {
        if (target == null) {
            return null;
        }

        var result = new BeginConditionTarget(
            Buff: ParseBuffs(target),
            Skill: ParseSkill(target),
            Event: ParseEvent(target));

        return DefaultBeginConditionTarget.Equals(result) ? null : result;

        BeginConditionTarget.HasBuff[] ParseBuffs(SubConditionTarget data) {
            if (data.hasBuffID.Length == 0 || data.hasBuffID[0] == 0) {
                return Array.Empty<BeginConditionTarget.HasBuff>();
            }

            var hasBuff = new BeginConditionTarget.HasBuff[data.hasBuffID.Length];
            for (int i = 0; i < hasBuff.Length; i++) {
                hasBuff[i] = new BeginConditionTarget.HasBuff(
                    Id: data.hasBuffID[i],
                    Level: (short) (data.hasBuffLevel.Length > i ? data.hasBuffLevel[i] : 0),
                    Owned: data.hasBuffOwner.Length > i && data.hasBuffOwner[i] != 0,
                    Count: data.hasBuffCount.Length > i ? data.hasBuffCount[i] : 0,
                    Compare: data.hasBuffCountCompare.Length > i ? Enum.Parse<CompareType>(data.hasBuffCountCompare[i]) : CompareType.Equals);
            }

            return hasBuff;
        }

        BeginConditionTarget.HasSkill? ParseSkill(SubConditionTarget data) {
            return data.hasSkillID > 0 ? new BeginConditionTarget.HasSkill(data.hasSkillID, data.hasSkillLevel) : null;
        }

        BeginConditionTarget.EventCondition? ParseEvent(SubConditionTarget data) {
            if (data.eventCondition == 0) {
                return null;
            }

            return new BeginConditionTarget.EventCondition(
                Type: data.eventCondition,
                IgnoreOwner: data.ignoreOwnerEvent != 0,
                SkillIds: data.eventSkillID,
                BuffIds: data.eventEffectID);
        }
    }
}
