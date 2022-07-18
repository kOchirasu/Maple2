using System;
using System.Collections.Generic;
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
    public bool Enabled => FireCount != 0 || Environment.TickCount <= endTick;
    public bool Active { get; private set; } = true;

    public readonly Vector3[] Points;
    public readonly bool UseDirection;
    private readonly int endTick;
    public int NextTick { get; private set; }

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
                    if (GetTargets(prisms, attack.Range.ApplyTarget, attack.TargetCount).Any()) {
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

        foreach (SkillMetadataMotion motion in Value.Data.Motions) {
            byte attackPoint = 0;
            foreach (SkillMetadataAttack attack in motion.Attacks) {
                Prism[] prisms = Points.Select(point => attack.Range.GetPrism(point, UseDirection ? Rotation.Z : 0)).ToArray();
                IActor[] targets = GetTargets(prisms, attack.Range.ApplyTarget, attack.TargetCount).ToArray();
                if (targets.Length > 0) {
                    Log.Debug("[{Tick}] {ObjectId}:{AttackPoint} Targeting: {Count}/{Limit} {Type}",
                        NextTick, ObjectId, attack.Point, targets.Length, attack.TargetCount, attack.Range.ApplyTarget);
                    foreach (IActor target in targets) {
                        Log.Debug("- {Id} @ {Position}", target.ObjectId, target.Position);
                    }
                }
                // else if (Caster.ObjectId != 0) { // Not Ms2RegionSkill
                //     Log.Debug("[{Tick}] {ObjectId}:{AttackPoint} has {Count}/{Limit} {Type}",
                //         NextTick, ObjectId, attack.Point, 0, attack.TargetCount, attack.Range.ApplyTarget);
                //     Log.Debug("{Range}", attack.Range);
                //     foreach (Prism prism in prisms) {
                //         Log.Debug("- {Prism}", prism);
                //     }
                // }

                if (attack.Damage.Count > 0) {
                    var damage = new DamageRecord {
                        CasterId = Caster.ObjectId,
                        OwnerId = ObjectId,
                        SkillId = Value.Id,
                        Level = Value.Level,
                        MotionPoint = attackPoint,
                    };

                    foreach (IActor target in targets) {
                        var targetRecord = new DamageRecordTarget {
                            ObjectId = target.ObjectId,
                        };
                        long damageAmount = 0;
                        for (int i = 0; i < attack.Damage.Count; i++) {
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

                foreach (SkillEffectMetadata effect in attack.Skills) {
                    if (effect.Condition != null) {
                        // ConditionSkill
                        switch (effect.Condition.Target) {
                            case SkillEntity.Owner:
                                foreach (IActor target in targets) {
                                    target.ApplyEffect(Caster, effect);
                                }
                                break;
                            default:
                                Log.Error("Invalid Target: {Target}", effect.Condition.Target);
                                break;
                        }
                    } else if (effect.Splash != null) {
                        Log.Debug("SkillSource Splash Skill untested");
                        Field.AddSkill(Caster, this, attack);
                    }
                }

                attackPoint++;
            }
        }

        FireCount--;
        NextTick = Environment.TickCount + Interval;
    }

    private IEnumerable<IActor> GetTargets(Prism[] prisms, SkillEntity entity, int limit) {
        switch (entity) {
            case SkillEntity.Owner:
            case SkillEntity.Attacker:
            case SkillEntity.RegionBuff:
            case SkillEntity.RegionDebuff:
                return prisms.Filter(Field.Players.Values, limit);
            case SkillEntity.Target:
                return prisms.Filter(Field.Npcs.Values, limit);
            default:
                Log.Debug("Unhandled SkillEntity:{Entity}", entity);
                return Array.Empty<IActor>();
        }
    }
}
