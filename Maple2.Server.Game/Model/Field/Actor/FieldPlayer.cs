using System;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Collision;

namespace Maple2.Server.Game.Model;

public class FieldPlayer : Actor<Player> {
    public readonly GameSession Session;
    public Vector3 LastGroundPosition;

    public override Stats Stats => Session.Stats.Values;
    public override IPrism Shape => new Prism(new Circle(new Vector2(Position.X, Position.Y), 10), Position.Z, 100);
    private int battleTick;
    private bool inBattle;

    public int TagId = 1;

    public FieldPlayer(GameSession session, Player player) : base(session.Field!, player.ObjectId, player) {
        Session = session;

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

    public virtual void TargetAttack(SkillRecord record, int[] targetIds) {
        SkillMetadataAttack attack = record.Attack;
        // Clear Targets just in case SkillRecord is being reused.
        record.Targets.Clear();
        foreach (int targetId in targetIds) {
            switch (attack.Range.ApplyTarget) {
                case SkillEntity.Target:
                    if (Field.Mobs.TryGetValue(targetId, out FieldNpc? npc)) {
                        record.Targets.Add(npc);
                    }
                    continue;
                case SkillEntity.Owner:
                    if (Field.TryGetPlayer(targetId, out FieldPlayer? player)) {
                        record.Targets.Add(player);
                    }
                    continue;
                case SkillEntity.RegionPet:
                    if (Field.Pets.TryGetValue(targetId, out FieldPet? pet)) {
                        record.Targets.Add(pet);
                    }
                    continue;
                default:
                    Logger.Debug("Unhandled Target-SkillEntity:{Entity}", attack.Range.ApplyTarget);
                    continue;
            }
        }

        if (record.Targets.Count == 0) {
            return;
        }

        var damage = new DamageRecord {
            TargetUid = record.TargetUid,
            OwnerId = ObjectId,
            SkillId = record.SkillId,
            Level = record.Level,
            AttackPoint = record.AttackPoint,
            MotionPoint = record.MotionPoint,
            Position = record.ImpactPosition,
            Direction = record.Direction,
        };

        foreach (IActor target in record.Targets) {
            target.ApplyDamage(this, damage, attack);
        }

        Field.Broadcast(SkillDamagePacket.Damage(damage));

        foreach (SkillEffectMetadata effect in attack.Skills) {
            if (effect.Condition != null) {
                foreach (IActor actor in record.Targets) {
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
