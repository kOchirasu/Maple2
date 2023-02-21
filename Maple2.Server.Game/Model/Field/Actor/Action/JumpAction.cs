using System.Numerics;
using Maple2.Server.Game.Model.State;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Model.Action;

public class JumpAction : NpcAction {
    private readonly Vector3 endPosition;

    public static JumpAction DistanceA(FieldNpc npc, int distance, float duration, float height) {
        Vector3 endPosition = npc.Position.Offset(distance, npc.Rotation);
        return new JumpAction(npc, -2, endPosition, duration, height);
    }

    public static JumpAction PositionA(FieldNpc npc, Vector3 endPosition, float duration) {
        return new JumpAction(npc, -2, endPosition, duration, 0.3f);
    }

    public static JumpAction InPlaceA(FieldNpc npc, float duration) {
        return new JumpAction(npc, -2, duration);
    }

    public static JumpAction DistanceB(FieldNpc npc, int distance, float duration, float height) {
        Vector3 endPosition = npc.Position.Offset(distance, npc.Rotation);
        return new JumpAction(npc, -3, endPosition, duration, height);
    }

    public static JumpAction PositionB(FieldNpc npc, Vector3 endPosition, float duration) {
        return new JumpAction(npc, -3, endPosition, duration, 0f);
    }

    public static JumpAction InPlaceB(FieldNpc npc, float duration) {
        return new JumpAction(npc, -3, duration);
    }

    private JumpAction(FieldNpc npc, short sequenceId, Vector3 endPosition, float duration, float height) : base(npc, sequenceId, duration) {
        this.endPosition = endPosition;
        Npc.StateData = new StateJumpNpc(Npc.Position, endPosition, duration, height);
    }

    private JumpAction(FieldNpc npc, short sequenceId, float duration) : base(npc, sequenceId, duration) {
        endPosition = Npc.Position;
        Npc.StateData = new StateJumpNpc(Npc.Position);
    }

    public override void OnCompleted() {
        if (Completed) {
            return;
        }

        base.OnCompleted();
        Npc.Position = Npc.Field.Navigation.UpdateAgent(Npc.Agent, endPosition);
        Npc.Velocity = default;
    }
}
