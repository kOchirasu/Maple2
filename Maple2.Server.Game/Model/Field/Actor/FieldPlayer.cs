using System;
using System.Numerics;
using Maple2.Model.Game;
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

    public override bool Sync() {
        return base.Sync();
    }

    protected override void OnDeath() {
        throw new NotImplementedException();
    }
}
