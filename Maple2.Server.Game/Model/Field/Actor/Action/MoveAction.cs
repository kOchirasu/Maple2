using System;
using System.Numerics;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Model.Action;

public class MoveAction : NpcAction {
    private long lastTick;

    public static MoveAction Walk(FieldNpc npc, short sequenceId, float duration) {
        return new MoveAction(npc, sequenceId, npc.Value.Metadata.Action.WalkSpeed, duration);
    }

    public static MoveAction Walk(FieldNpc npc, short sequenceId, int distance) {
        return Walk(npc, sequenceId, distance / npc.Value.Metadata.Action.WalkSpeed);
    }

    public static MoveAction Run(FieldNpc npc, short sequenceId, float duration) {
        return new MoveAction(npc, sequenceId, npc.Value.Metadata.Action.RunSpeed, duration);
    }

    public static MoveAction Run(FieldNpc npc, short sequenceId, int distance) {
        return Walk(npc, sequenceId, distance / npc.Value.Metadata.Action.RunSpeed);
    }

    private MoveAction(FieldNpc npc, short sequenceId, float speed, float duration) : base(npc, sequenceId, duration) {
        npc.Velocity = speed * Vector3.UnitY;
        lastTick = Environment.TickCount64;
    }

    public override bool Update(long tickCount) {
        float timeDelta = (tickCount - lastTick) / 1000f;
        Vector3 distanceVector = timeDelta * Npc.Velocity.Rotate(Npc.Rotation);
        Npc.Position += distanceVector;

        lastTick = tickCount;
        return base.Update(tickCount);
    }

    public override void OnCompleted() {
        if (Completed) {
            return;
        }

        base.OnCompleted();
        Npc.Position = Npc.Field.Navigation.UpdateAgent(Npc.Agent, Npc.Position);
        Npc.Velocity = default;
    }
}
