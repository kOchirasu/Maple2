using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Serilog;

namespace Maple2.Server.Game.Model;

public class FieldPlayer : Actor<Player> {
    public readonly GameSession Session;

    public override Stats Stats => Session.Stats.Values;
    public bool InBattle;

    public int TagId = 1;

    public FieldPlayer(GameSession session, Player player) : base(session.Field!, player.ObjectId, player) {
        Session = session;

        Scheduler.ScheduleRepeated(() => Field.Broadcast(ProxyObjectPacket.UpdatePlayer(this, 66)), 2000);
        Scheduler.Start();
    }

    public static implicit operator Player(FieldPlayer fieldPlayer) => fieldPlayer.Value;

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

                Field.Broadcast(StatsPacket.Update(this, updated.ToArray()));
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
