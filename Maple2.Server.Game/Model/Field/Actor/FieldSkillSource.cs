using System;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Util;
using Maple2.Tools.Collision;
using Serilog;

namespace Maple2.Server.Game.Model;

public class FieldSkillSource : ActorBase<SkillMetadata> {
    public int Interval { get; init; } = 1000;

    private int nextTick;

    public FieldSkillSource(FieldManager field, int objectId, SkillMetadata value) : base(field, objectId, value) {
        nextTick = Environment.TickCount;
    }

    public override void Sync() {
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
                Prism prism = attack.Range.GetPrism(Position, Rotation.Z);
                FieldPlayer[] players = prism.Filter(Field.Players.Values, attack.TargetCount).ToArray();

                if (attack.Damage.Count > 0) {
                    Log.Debug("SkillSource Damage unimplemented");
                    var record = new DamageRecord();
                    foreach (FieldPlayer player in players) {
                        var targetRecord = new DamageRecordTarget {
                            ObjectId = player.ObjectId,
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
                            case SkillEntity.Target:
                                foreach (FieldPlayer player in players) {
                                    player.ApplyEffect(this, effect);
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

        nextTick = Environment.TickCount + Interval;
    }
}
