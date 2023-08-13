using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Formulas;
using Maple2.Server.Game.Manager.Config;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Util;
using Maple2.Tools.Collision;
using Serilog;

namespace Maple2.Server.Game.Model;

/// <summary>
/// Actor is an entity that can engage in combat.
/// </summary>
/// <typeparam name="T">The type contained by this object</typeparam>
public abstract class Actor<T> : IActor<T>, IDisposable {

    protected readonly ILogger Logger = Log.ForContext<T>();

    public FieldManager Field { get; }
    public T Value { get; }

    public virtual Stats Stats { get; } = new(0, 0);

    public int ObjectId { get; }
    public virtual Vector3 Position { get; set; }
    public virtual Vector3 Rotation { get; set; }

    public virtual bool IsDead { get; protected set; }
    public abstract IPrism Shape { get; }

    public BuffManager Buffs { get; }

    protected Actor(FieldManager field, int objectId, T value) {
        Field = field;
        ObjectId = objectId;
        Value = value;
        Buffs = new BuffManager(this);
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }

    public virtual void ApplyEffect(IActor caster, IActor owner, SkillEffectMetadata effect, bool notifyField = true) {
        Debug.Assert(effect.Condition != null);

        foreach (SkillEffectMetadata.Skill skill in effect.Skills) {
            Buffs.AddBuff(caster, owner, skill.Id, skill.Level, notifyField);
        }
    }

    public virtual void ApplyDamage(IActor caster, DamageRecord damage, SkillMetadataAttack attack) {
        if (attack.Damage.Count > 0) {
            var targetRecord = new DamageRecordTarget {
                ObjectId = ObjectId,
                Position = caster.Position,
                Direction = caster.Rotation, // Idk why this is wrong
            };

            long damageAmount = 0;
            for (int i = 0; i < attack.Damage.Count; i++) {
                Reflect(caster);
                (DamageType, double) damageHit = GetDamage(caster, damage);
                targetRecord.AddDamage(damageHit.Item1, (long) damageHit.Item2);
                damageAmount -= (long) damageHit.Item2;
            }

            if (damageAmount != 0) {
                Stats[BasicAttribute.Health].Add(damageAmount);
                Field.Broadcast(StatsPacket.Update(this, BasicAttribute.Health));
            }

            damage.Targets.Add(targetRecord);
        }
    }

    public virtual void Reflect(IActor target) {
        if (Buffs.Reflect == null || Buffs.Reflect.Counter >= Buffs.Reflect.Metadata.Count) {
            return;
        }
        ReflectRecord record = Buffs.Reflect;

        if (record.Metadata.Rate is not 1 && record.Metadata.Rate < Random.Shared.NextDouble()) {
            return;
        }

        record.Counter++;
        if (record.Counter >= record.Metadata.Count) {
            Buffs.Remove(record.SourceBuffId);
        }
        target.Buffs.AddBuff(this, target, record.Metadata.EffectId, record.Metadata.EffectLevel);

        // TODO: Reflect should also amend the target's damage record from Reflect.ReflectValues and ReflectRates
    }

    public virtual void TargetAttack(SkillRecord record) {
        if (record.Targets.Count == 0) {
            return;
        }

        var damage = new DamageRecord(record.Metadata, record.Attack) {
            CasterId = record.Caster.ObjectId,
            TargetUid = record.TargetUid,
            OwnerId = record.Caster.ObjectId,
            SkillId = record.SkillId,
            Level = record.Level,
            AttackPoint = record.AttackPoint,
            MotionPoint = record.MotionPoint,
            Position = record.ImpactPosition,
            Direction = record.Direction,
        };

        foreach (IActor target in record.Targets) {
            target.ApplyDamage(this, damage, record.Attack);
        }

        Field.Broadcast(SkillDamagePacket.Damage(damage));

        foreach (SkillEffectMetadata effect in record.Attack.Skills) {
            if (effect.Condition != null) {
                foreach (IActor actor in record.Targets) {
                    // Condition check here is redundant. It's already checked in ApplyEffect
                    if (effect.Condition.Condition.Check(record.Caster, actor, actor)) {
                        actor.ApplyEffect(GetTarget(effect.Condition.Target, record.Caster, actor),
                            GetTarget(effect.Condition.Target, record.Caster, actor), effect);
                    }
                }
            } else if (effect.Splash != null) {
                Field.AddSkill(record.Caster, effect, new[] {
                    record.Caster.Position,
                }, record.Caster.Rotation);
            }
        }
    }

    public virtual void ApplyEffect(IActor caster, IActor target, SkillEffectMetadata effect) {
        if (effect.Condition != null && !effect.Condition.Condition.Check(caster, target, target)) {
            return;
        }


        foreach (SkillEffectMetadata.Skill skill in effect.Skills) {
            Buffs.AddBuff(caster, target, skill.Id, skill.Level);
        }
    }

    public virtual void Update(long tickCount) {
        if (IsDead) return;

        if (Stats[BasicAttribute.Health].Current <= 0) {
            IsDead = true;
            OnDeath();
            return;
        }

        Buffs.Update(tickCount);
    }

    protected abstract void OnDeath();

    public (DamageType, long) GetDamage(IActor caster, DamageRecord record) {
        // Check block
        float blockRate = Buffs.TotalCompulsionRate(CompulsionEventType.BlockChance, record.SkillId);
        if (blockRate >= 1 || (blockRate > 0 && Random.Shared.NextDouble() <= blockRate)) {
            return (DamageType.Block, 0);
        }

        // Check evade
        float evadeRate = Buffs.TotalCompulsionRate(CompulsionEventType.EvasionChanceOverride, record.SkillId);
        if (evadeRate > 0 && Random.Shared.NextDouble() <= evadeRate) {
            return (DamageType.Miss, 0);
        }

        // Get hit rate
        double hitRate = Damage.HitRate(this.Buffs.GetResistance(BasicAttribute.Accuracy), this.Stats[BasicAttribute.Evasion].Total, this.Stats[BasicAttribute.Dexterity].Total,
            caster.Stats[BasicAttribute.Accuracy].Total, caster.Buffs.GetResistance(BasicAttribute.Evasion), caster.Stats[BasicAttribute.Luck].Total);

        if (Random.Shared.NextDouble() > hitRate) {
            return (DamageType.Miss, 0);
        }

        double luckCoefficient = 1;
        double attackDamage = 0;
        DamageType damageType = DamageType.Normal;

        if (caster is FieldPlayer casterPlayer) {
            luckCoefficient = Damage.LuckCoefficient(casterPlayer.Value.Character.Job.Code());

            double bonusAttack = casterPlayer.Stats[BasicAttribute.BonusAtk].Total + Constant.PetAttackMultiplier * casterPlayer.Stats[BasicAttribute.PetBonusAtk].Total;
            double bonusAttackResistance = 1 / (1 + this.Buffs.GetResistance(BasicAttribute.BonusAtk));
            double weaponAttackWeakness = 1 / (1 + this.Buffs.GetResistance(BasicAttribute.MaxWeaponAtk));
            double bonusAttackCoefficient = bonusAttackResistance * BonusAttackCoefficient(casterPlayer);
            double minDamage = weaponAttackWeakness * casterPlayer.Stats[BasicAttribute.MinWeaponAtk].Total + bonusAttackCoefficient * bonusAttack;
            double maxDamage = weaponAttackWeakness * casterPlayer.Stats[BasicAttribute.MaxWeaponAtk].Total + bonusAttackCoefficient * bonusAttack;

            attackDamage = minDamage + (maxDamage - minDamage) * Random.Shared.NextDouble();
        }

        // change the NPCNormalDamage to be changed depending on target? 
        double damageBonus = 1 + (caster.Stats[SpecialAttribute.TotalDamage].Multiplier()) + (caster.Stats[SpecialAttribute.NormalNpcDamage].Multiplier());
        if (this is FieldNpc npc && npc.Value.IsBoss) {
            damageBonus += caster.Stats[SpecialAttribute.BossNpcDamage].Multiplier();
        }

        // Get elemental bonus
        switch (record.SkillMetadata.Property.Element) {
            case Element.Fire:
                damageBonus += caster.Stats[SpecialAttribute.FireDamage].Multiplier();
                break;
            case Element.Ice:
                damageBonus += caster.Stats[SpecialAttribute.IceDamage].Multiplier();
                break;
            case Element.Electric:
                damageBonus += caster.Stats[SpecialAttribute.ElectricDamage].Multiplier();
                break;
            case Element.Holy:
                damageBonus += caster.Stats[SpecialAttribute.HolyDamage].Multiplier();
                break;
            case Element.Dark:
                damageBonus += caster.Stats[SpecialAttribute.DarkDamage].Multiplier();
                break;
            case Element.Poison:
                damageBonus += caster.Stats[SpecialAttribute.PoisonDamage].Multiplier();
                break;
        }

        // Get range bonus
        switch (record.SkillMetadata.Property.RangeType) {
            case RangeType.None:
                break;
            case RangeType.Melee:
                damageBonus += caster.Stats[SpecialAttribute.MeleeDamage].Multiplier();
                break;
            case RangeType.Range:
                damageBonus += caster.Stats[SpecialAttribute.RangedDamage].Multiplier();
                break;
        }

        damageBonus += -this.Buffs.GetResistance(BasicAttribute.AttackSpeed) * caster.Stats[BasicAttribute.AttackSpeed].Total;

        // Check for crit and get crit damage
        /* TODO: DotDamage can be be NOT crit. Player skills yes. Will need to implement this */
        if (record.AttackMetadata.CompulsionTypes.Contains(CompulsionType.Critical)) {
            damageType = DamageType.Critical;
        }

        if (damageType != DamageType.Critical) {
            damageType = Damage.RollCritical(caster.Stats[BasicAttribute.Luck].Total * luckCoefficient,
                caster.Stats[BasicAttribute.CriticalRate].Total, caster.Buffs.TotalCompulsionRate(CompulsionEventType.CritChanceOverride, record.SkillId),
                this.Stats[BasicAttribute.CriticalEvasion].Total);
        }

        double finalCritDamage = damageType == DamageType.Critical ? Damage.FinalCriticalDamage(this.Buffs.GetResistance(BasicAttribute.CriticalDamage), caster.Stats[BasicAttribute.CriticalDamage].Total) : 1;
        damageBonus *= finalCritDamage;

        // Get invoked buff values
        // TODO: Need to make this flexible to get invoke values from skills or buffs. If buff -> InvokeEffectType.IncreaseDotDamage
        (int, float) invokeValues = caster.Buffs.GetInvokeValues(InvokeEffectType.IncreaseSkillDamage, record.SkillId, record.SkillMetadata.Property.SkillGroup);

        double damageMultiplier = damageBonus * (1 + invokeValues.Item2) * (record.AttackMetadata.Damage.Rate + invokeValues.Item1);

        double defensePierce = 1 - Math.Min(0.3, (1 / (1 + this.Buffs.GetResistance(BasicAttribute.Piercing)) * (caster.Stats[BasicAttribute.Piercing].Multiplier() - 1)));
        damageMultiplier *= 1 / (Math.Max(this.Stats[BasicAttribute.Defense].Total, 1) * defensePierce);

        // Check resistances
        double attackTypeAmount = 0;
        double resistance = 0;
        double finalDamage = 0;
        switch (record.SkillMetadata.Property.AttackType) {
            case AttackType.Physical:
                resistance = Damage.CalculateResistance(this.Stats[BasicAttribute.PhysicalRes].Total, caster.Stats[SpecialAttribute.PhysicalPiercing].Multiplier());
                attackTypeAmount = caster.Stats[BasicAttribute.PhysicalAtk].Total;
                finalDamage = caster.Stats[SpecialAttribute.OffensivePhysicalDamage].Multiplier();
                break;
            case AttackType.Magic:
                resistance = Damage.CalculateResistance(this.Stats[BasicAttribute.MagicalRes].Total, caster.Stats[SpecialAttribute.MagicalPiercing].Multiplier());
                attackTypeAmount = caster.Stats[BasicAttribute.MagicalAtk].Total;
                finalDamage = caster.Stats[SpecialAttribute.OffensiveMagicalDamage].Multiplier();
                break;
            // If all, calculate the higher of the two
            case AttackType.All:
                BasicAttribute attackTypeAttribute = caster.Stats[BasicAttribute.PhysicalAtk].Total >= caster.Stats[BasicAttribute.MagicalAtk].Total
                    ? BasicAttribute.PhysicalAtk
                    : BasicAttribute.MagicalAtk;
                SpecialAttribute piercingAttribute = attackTypeAttribute == BasicAttribute.PhysicalAtk
                    ? SpecialAttribute.PhysicalPiercing
                    : SpecialAttribute.MagicalPiercing;
                SpecialAttribute finalDamageAttribute = attackTypeAttribute == BasicAttribute.PhysicalAtk
                    ? SpecialAttribute.OffensivePhysicalDamage
                    : SpecialAttribute.OffensiveMagicalDamage;
                attackTypeAmount = Math.Max(caster.Stats[BasicAttribute.PhysicalAtk].Total, caster.Stats[BasicAttribute.MagicalAtk].Total) * 0.5f;
                resistance = Damage.CalculateResistance(this.Stats[attackTypeAttribute].Total, caster.Stats[piercingAttribute].Multiplier());
                finalDamage = caster.Stats[finalDamageAttribute].Multiplier();
                break;
        }

        damageMultiplier *= attackTypeAmount * resistance * finalDamage;
        attackDamage *= damageMultiplier * Constant.AttackDamageFactor + record.AttackMetadata.Damage.Value;

        // Apply any shields
        foreach (Buff buff in Buffs.Buffs.Values.Where(buff => buff.Metadata.Shield != null)) {
            if (buff.ShieldHealth >= attackDamage) {
                buff.ShieldHealth -= (long) attackDamage;
                attackDamage = 0;
                Field.Broadcast(BuffPacket.Update(buff, BuffFlag.UpdateBuff));
                return (DamageType.Block, (long) attackDamage);
            }

            attackDamage -= buff.ShieldHealth;
            Buffs.Remove(buff.Id);
        }

        return (damageType, (long) attackDamage);

        double BonusAttackCoefficient(FieldPlayer player) {
            int leftHandRarity = player.Session.Item.Equips.Get(EquipSlot.RH)?.Rarity ?? 0;
            int rightHandRarity = player.Session.Item.Equips.Get(EquipSlot.LH)?.Rarity ?? 0;
            return BonusAttack.Coefficient(rightHandRarity, leftHandRarity, player.Value.Character.Job.Code());
        }
    }

    private IActor GetTarget(SkillEntity entity, IActor caster, IActor target) {
        return entity switch {
            SkillEntity.Target => target,
            SkillEntity.Owner => target,
            SkillEntity.Caster => caster,
            _ => throw new NotImplementedException(),
        };
    }
}
