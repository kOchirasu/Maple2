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

public class FieldSkill : FieldEntity<SkillMetadata>, IOwned {
    public IActor Owner { get; init; }
    public int Interval { get; init; } = 1000;
    public int FireCount { get; private set; } = -1;
    public bool UseRotation { get; init; }
    public bool Enabled => FireCount != 0;

    public readonly Vector3[] Points;
    private int nextTick;

    public FieldSkill(FieldManager field, int objectId, IActor owner, SkillMetadata value, params Vector3[] points) : base(field, objectId, value) {
        Owner = owner;
        Points = points;
        nextTick = Environment.TickCount;
    }

    public void SetFireCount(int fireCount) {
        FireCount = fireCount;
    }

    public override void Sync() {
        if (!Enabled) {
            Field.RemoveSkill(ObjectId);
            return;
        }

        if (Environment.TickCount < nextTick) {
            return;
        }

        // TODO: These are buffs? seems irrelevant to FieldSkill?
        // foreach (SkillEffectMetadata skill in Value.Data.Skills) {
        //     if (skill.Condition != null) {
        //         ConditionSkill(Field.Players.Values, skill);
        //     } else if (skill.Splash != null) {
        //         SplashSkill(skill);
        //     }
        // }

        foreach (SkillMetadataMotion motion in Value.Data.Motions) {
            foreach (SkillMetadataAttack attack in motion.Attacks) {
                Prism[] prisms = Points.Select(point => attack.Range.GetPrism(point, UseRotation ? Rotation.Z : 0)).ToArray();
                IActor[] targets;
                switch (attack.Range.ApplyTarget) {
                    case SkillEntity.Player:
                    case SkillEntity.Attacker:
                    case SkillEntity.RegionBuff:
                    case SkillEntity.RegionDebuff:
                        targets = prisms.Filter(Field.Players.Values, attack.TargetCount).ToArray();
                        break;
                    default:
                        Log.Debug("Unhandled SkillEntity:{Entity}", attack.Range.ApplyTarget);
                        continue;
                }

                if (targets.Length > 0) {
                    Log.Debug("[{Tick}] {ObjectId}:{AttackPoint} Targeting: {Count} players", nextTick, ObjectId, attack.Point, targets.Length);
                    foreach (IActor target in targets) {
                        Log.Debug("- {Id} @ {Position}", target.ObjectId, target.Position);
                    }
                }

                if (attack.Damage.Count > 0) {
                    Log.Debug("SkillSource Damage unimplemented");
                    var record = new DamageRecord {
                        SkillId = Value.Id,
                        Level = Value.Level,
                    };
                    foreach (IActor target in targets) {
                        var targetRecord = new DamageRecordTarget {
                            ObjectId = target.ObjectId,
                            Direction = default,
                            Position = default,
                        };
                        for (int i = 0; i < attack.Damage.Count; i++) {
                            targetRecord.AddDamage(DamageType.Normal, i);
                        }
                        record.Targets.Add(targetRecord);
                    }

                    Field.Broadcast(SkillDamagePacket.Damage(record));
                }

                foreach (SkillEffectMetadata effect in attack.Skills) {
                    if (effect.Condition != null) {
                        // ConditionSkill
                        switch (effect.Condition.Target) {
                            case SkillEntity.Player:
                                foreach (IActor target in targets) {
                                    target.ApplyEffect(Owner, effect);
                                }
                                break;
                            default:
                                Log.Error("Invalid Target: {Target}", effect.Condition.Target);
                                break;
                        }
                    } else if (effect.Splash != null) {
                        Log.Debug("SkillSource Splash Skill unimplemented");
                    }
                }
            }
        }

        FireCount--;
        nextTick = Environment.TickCount + Interval;
    }
}
