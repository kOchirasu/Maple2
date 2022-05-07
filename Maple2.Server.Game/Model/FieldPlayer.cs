using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Model;

public class FieldPlayer : IActor<Player> {
    public readonly GameSession Session;

    public int ObjectId { get; }
    public Player Value { get; }

    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public PlayerState State { get; set; }
    public PlayerSubState SubState { get; set; }

    public IReadOnlyDictionary<int, Buff> Buffs => new Dictionary<int, Buff>();
    public Stats Stats => new Stats();
    public bool InBattle;

    public FieldPlayer(int objectId, GameSession session, Player player) {
        ObjectId = objectId;
        Session = session;
        Value = player;
    }

    public static implicit operator Player(FieldPlayer fieldPlayer) => fieldPlayer.Value;
}
