﻿using System.Numerics;
using Maple2.Model.Game;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Model; 

public class FieldPlayer : IActor<Player> {
    public readonly GameSession Session;
    
    public int ObjectId { get; init; }
    public Player Value { get; init; }
    
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public FieldPlayer(int objectId, GameSession session, Player player) {
        ObjectId = objectId;
        Session = session;
        Value = player;
    }

    public static implicit operator Player(FieldPlayer fieldPlayer) => fieldPlayer.Value;
}