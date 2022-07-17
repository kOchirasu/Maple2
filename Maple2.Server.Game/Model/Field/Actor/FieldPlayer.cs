using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Model;

public class FieldPlayer : Actor<Player> {
    public readonly GameSession Session;

    public override Stats Stats => Session.Stats.Values;
    private int battleTick;
    private bool inBattle;

    public int TagId = 1;

    public FieldPlayer(GameSession session, Player player) : base(session.Field!, player.ObjectId, player) {
        Session = session;
        BroadcastBuffs = true;

        Scheduler.ScheduleRepeated(() => Field.Broadcast(ProxyObjectPacket.UpdatePlayer(this, 66)), 2000);
        Scheduler.ScheduleRepeated(() => {
            if (InBattle && Environment.TickCount - battleTick > 2000) {
                InBattle = false;
            }
        }, 500);
        Scheduler.Start();
    }

    public static implicit operator Player(FieldPlayer fieldPlayer) => fieldPlayer.Value;

    public bool InBattle {
        get => inBattle;
        set {
            if (value != inBattle) {
                inBattle = value;
                Session.Field?.Broadcast(SkillPacket.InBattle(this));
            }

            if (inBattle) {
                battleTick = Environment.TickCount;
            }
        }
    }

    public void TargetAttack(SkillRecord record, int[] targetIds) {
        SkillMetadataAttack attack = record.Attack;
        var targets = new List<IActor>();
        foreach (int targetId in targetIds) {
            switch (attack.Range.ApplyTarget) {
                case SkillEntity.Enemy:
                    if (Field.TryGetNpc(targetId, out FieldNpc? npc)) {
                        targets.Add(npc);
                    }
                    continue;
                case SkillEntity.Player:
                    if (Field.TryGetPlayer(targetId, out FieldPlayer? player)) {
                        targets.Add(player);
                    }
                    continue;
                default:
                    logger.Debug("Unhandled Target-SkillEntity:{Entity}", attack.Range.ApplyTarget);
                    continue;
            }
        }

        if (targets.Count == 0) {
            return;
        }

        if (attack.Damage.Count > 0) {
            var damage = new DamageRecord {
                AttackCounter = 1,
                CasterId = ObjectId,
                OwnerId = ObjectId,
                SkillId = record.SkillId,
                Level = record.Level,
                AttackPoint = record.AttackPoint,
                MotionPoint = record.MotionPoint,
                Position = record.Position,
                // Rotation = record.Rotation,
                Direction = record.Direction,
            };

            foreach (IActor target in targets) {
                var targetRecord = new DamageRecordTarget {ObjectId = target.ObjectId};
                long damageAmount = 0;
                for (int i = 0; i < attack.Damage.Count; i++) {
                    targetRecord.AddDamage(DamageType.Normal, -20);
                    damageAmount -= 20;
                }

                if (damageAmount != 0) {
                    target.Stats[StatAttribute.Health].Add(damageAmount);
                    Field.Broadcast(StatsPacket.Update(target, StatAttribute.Health));
                }

                damage.Targets.Add(targetRecord);
            }

            Field.Broadcast(SkillDamagePacket.Damage(damage));
        }

        foreach (SkillEffectMetadata effect in attack.Skills) {
            if (effect.Condition != null) {
                foreach (IActor actor in targets) {
                    actor.ApplyEffect(this, effect);
                }
            } else if (effect.Splash != null) {
                // Handled by SplashAttack?
            }
        }
    }

    public override void Sync() {
        base.Sync();
    }

    protected override void OnDeath() {
        throw new NotImplementedException();
    }
}
