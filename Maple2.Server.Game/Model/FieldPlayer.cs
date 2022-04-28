using System.Numerics;

namespace Maple2.Server.Game.Field; 

public class FieldPlayer : IActor<Model.Player> {
    public int ObjectId { get; init; }
    public Model.Player Value { get; init; }
    
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public FieldPlayer(int objectId, Model.Player player) {
        ObjectId = objectId;
        Value = player;
    }

    public static implicit operator Model.Player(FieldPlayer fieldPlayer) => fieldPlayer.Value;
}
