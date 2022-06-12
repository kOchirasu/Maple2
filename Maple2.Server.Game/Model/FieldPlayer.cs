using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Model;

public class FieldPlayer : Actor<Player> {
    public readonly GameSession Session;

    public override IReadOnlyDictionary<int, Buff> Buffs => new Dictionary<int, Buff>();
    public override Stats Stats => Session.Stats.Values;
    public bool InBattle;

    public FieldPlayer(int objectId, GameSession session, Player player) : base (session.Field!, objectId, player) {
        Session = session;
    }

    public static implicit operator Player(FieldPlayer fieldPlayer) => fieldPlayer.Value;

    public override void Sync() {
        base.Sync();
    }
}
