using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Serilog;

namespace Maple2.Server.Game.Model;

public class FieldPlayer : Actor<Player> {
    public readonly GameSession Session;

    public override Stats Stats => Session.Stats.Values;
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

    public void CastMagic(SkillRecord record, IReadOnlyList<MagicPath> magicPaths) {
        var points = new Vector3[magicPaths.Count];
        for (int i = 0; i < magicPaths.Count; i++) {
            MagicPath magicPath = magicPaths[i];
            Vector3 rotation = default;
            if (magicPath.Rotate) {
                rotation = Rotation;
            }

            Vector3 position = Position.Offset(magicPath.FireOffset, rotation);
            points[i] = magicPath.IgnoreAdjust ? position : position.Align();
        }

        foreach (SkillEffectMetadata attack in record.Attack.Skills) {
            Debug.Assert(attack.Splash != null);
            Field.AddSkill(this, attack, points, Position, Rotation);
        }
        //
        // var cubes = new IPolygon[points.Length];
        // for (int i = 0; i < points.Length; i++) {
        //     cubes[i] = new BoundingBox(
        //         new Vector2(points[i].X, points[i].Y),
        //         new Vector2(points[i].X  + 150f, points[i].Y + 150f));
        // }
        //
        // var prism = new CompositePrism(cubes, Position.Align().Z, 150f);
    }

    public override void Sync() {
        base.Sync();

        foreach ((int id, Buff buff) in buffs) {
            if (!buff.Enabled) {
                if (buffs.Remove(id, out _)) {
                    Field.Broadcast(BuffPacket.Remove(buff));
                }
            }

            if (!buff.ShouldProc()) {
                continue;
            }

            if (buff.Metadata.Recovery != null) {
                var record = new HealDamageRecord(buff.Caster, buff.Target, buff.ObjectId, buff.Metadata.Recovery);
                var updated = new List<StatAttribute>(3);
                if (record.HpAmount != 0) {
                    Stats[StatAttribute.Health].Add(record.HpAmount);
                    updated.Add(StatAttribute.Health);
                }
                if (record.SpAmount != 0) {
                    Stats[StatAttribute.Spirit].Add(record.SpAmount);
                    updated.Add(StatAttribute.Spirit);
                }
                if (record.EpAmount != 0) {
                    Stats[StatAttribute.Stamina].Add(record.EpAmount);
                    updated.Add(StatAttribute.Stamina);
                }

                if (updated.Count > 0) {
                    Field.Broadcast(StatsPacket.Update(this, updated.ToArray()));
                }
                Field.Broadcast(SkillDamagePacket.Heal(record));
            }

            if (buff.Metadata.Dot.Damage != null) {
                Log.Information("Actor DotDamage unimplemented");
            }

            if (buff.Metadata.Dot.Buff != null) {
                Log.Information("Actor DotBuff unimplemented");
            }

            foreach (SkillEffectMetadata skill in buff.Metadata.Skills) {
                Log.Information("Actor Skill Effect unimplemented");
            }
        }
    }
}
