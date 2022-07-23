using System;
using System.Linq;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Util;
using Maple2.Tools.Collision;
using Serilog;

namespace Maple2.Server.Game.Model;

public class FieldSkill : FieldEntity<SkillMetadata> {
    public IActor Caster { get; init; }
    public int Interval { get; }
    public int FireCount { get; private set; }
    public bool Enabled => FireCount < 0 || NextTick <= endTick || Environment.TickCount <= endTick;
    public bool Active { get; private set; } = true;

    public readonly Vector3[] Points;
    public readonly bool UseDirection;
    private readonly int endTick;
    public int NextTick { get; private set; }

    private readonly ILogger logger = Log.ForContext<FieldSkill>();

    public FieldSkill(FieldManager field, int objectId, IActor caster,
                      SkillMetadata value, int interval, params Vector3[] points) : base(field, objectId, value) {
        Caster = caster;
        Points = points;
        Interval = interval;
        FireCount = -1;
        NextTick = Environment.TickCount + interval;
    }

    public FieldSkill(FieldManager field, int objectId, IActor caster,
                      SkillMetadata value, int fireCount, SkillEffectMetadataSplash splash, params Vector3[] points) : base(field, objectId, value) {
        Caster = caster;
        Points = points;
        Interval = splash.Interval;
        FireCount = fireCount;
        UseDirection = splash.UseDirection;

        int baseTick = Environment.TickCount;
        if (splash.ImmediateActive) {
            NextTick = baseTick;
            endTick = baseTick + splash.RemoveDelay + (FireCount - 1) * splash.Interval;
        } else {
            NextTick = baseTick + splash.Delay + splash.Interval;
            endTick = baseTick + splash.Delay + splash.RemoveDelay + FireCount * splash.Interval;
        }
        if (splash.OnlySensingActive) {
            endTick = baseTick + splash.Delay + splash.RemoveDelay;
            Active = false;
        }
    }

    public override void Sync() {
        if (!Enabled) {
            Field.RemoveSkill(ObjectId);
            return;
        }

        if (Environment.TickCount < NextTick) {
            return;
        }

        // Check Activation for OnlySensingActive
        if (!Active) {
            foreach (SkillMetadataMotion motion in Value.Data.Motions) {
                foreach (SkillMetadataAttack attack in motion.Attacks) {
                    Prism[] prisms = Points.Select(point => attack.Range.GetPrism(point, UseDirection ? Rotation.Z : 0)).ToArray();
                    if (Field.GetTargets(prisms, attack.Range.ApplyTarget, attack.TargetCount).Any()) {
                        Active = true;
                        goto activated;
                    }
                }
            }

            NextTick = Environment.TickCount + Interval;
            return;
        }

    activated:
        // TODO: These are buffs? seems irrelevant to FieldSkill?
        // foreach (SkillEffectMetadata skill in Value.Data.Skills) {
        //     if (skill.Condition != null) {
        //         ConditionSkill(Field.Players.Values, skill);
        //     } else if (skill.Splash != null) {
        //         SplashSkill(skill);
        //     }
        // }

        var record = new SkillRecord(Value, 0, Caster) {
            Position = Position,
            Rotation = Rotation,
        };
        for (byte i = 0; record.TrySetMotionPoint(i); i++) {
            for (byte j = 0; record.TrySetAttackPoint(j); j++) {
                SkillMetadataAttack attack = record.Attack;
                Prism[] prisms = Points.Select(point => attack.Range.GetPrism(point, UseDirection ? Rotation.Z : 0)).ToArray();
                IActor[] targets = Field.GetTargets(prisms, attack.Range.ApplyTarget, attack.TargetCount).ToArray();
                // if (targets.Length > 0) {
                //     logger.Debug("[{Tick}] {ObjectId}:{AttackPoint} Targeting: {Count}/{Limit} {Type}",
                //         NextTick, ObjectId, attack.Point, targets.Length, attack.TargetCount, attack.Range.ApplyTarget);
                //     foreach (IActor target in targets) {
                //         logger.Debug("- {Id} @ {Position}", target.ObjectId, target.Position);
                //     }
                // }
                // else if (Caster.ObjectId != 0) { // Not Ms2RegionSkill
                //     logger.Debug("[{Tick}] {ObjectId}:{AttackPoint} has {Count}/{Limit} {Type}",
                //         NextTick, ObjectId, attack.Point, 0, attack.TargetCount, attack.Range.ApplyTarget);
                //     logger.Debug("{Range}", attack.Range);
                //     foreach (Prism prism in prisms) {
                //         logger.Debug("- {Prism}", prism);
                //     }
                // }

                if (attack.Damage.Count > 0) {
                    var damage = new DamageRecord {
                        CasterId = Caster.ObjectId,
                        OwnerId = ObjectId,
                        SkillId = Value.Id,
                        Level = Value.Level,
                        MotionPoint = record.MotionPoint,
                        AttackPoint = record.AttackPoint,
                    };

                    foreach (IActor target in targets) {
                        var targetRecord = new DamageRecordTarget {
                            ObjectId = target.ObjectId,
                            // TODO: These should be from the block that did damage?
                            Position = target.Position,  // Of block
                            Direction = target.Rotation, // Of block
                        };
                        long damageAmount = 0;
                        for (int k = 0; k < attack.Damage.Count; k++) {
                            targetRecord.AddDamage(DamageType.Normal, -10);
                            damageAmount -= 10;
                        }

                        if (damageAmount != 0) {
                            target.Stats[StatAttribute.Health].Add(damageAmount);
                            Field.Broadcast(StatsPacket.Update(target, StatAttribute.Health));
                        }

                        damage.Targets.Add(targetRecord);
                    }

                    Field.Broadcast(SkillDamagePacket.Region(damage));
                }

                foreach (SkillEffectMetadata effect in attack.Skills.Where(effect => effect.Condition != null)) {
                    if (effect.Condition == null) {
                        logger.Fatal("Invalid Condition-Skill being handled: {Effect}", effect);
                        continue;
                    }

                    switch (effect.Condition.Target) {
                        case SkillEntity.Owner:
                            foreach (IActor target in targets) {
                                target.ApplyEffect(Caster, effect);
                            }
                            break;
                        case SkillEntity.Target:
                            Caster.ApplyEffect(Caster, effect);
                            break;
                        default:
                            logger.Error("Invalid FieldSkill Target: {Target}", effect.Condition.Target);
                            break;
                    }
                }

                if (attack.Skills.Any(effect => effect.Splash != null)) {
                    Field.AddSkill(record);
                }
            }
        }

        if (Interval == 0) {
            FireCount = 0;
            NextTick = int.MaxValue;
        } else {
            FireCount--;
            NextTick += Interval;
        }
    }
}
