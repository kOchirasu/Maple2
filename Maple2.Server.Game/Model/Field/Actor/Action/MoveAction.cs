using System;
using System.Numerics;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Model.Action;

public class MoveAction : NpcAction {
    private int lastTick;

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
        lastTick = Environment.TickCount;
    }

    public override bool Sync() {
        int tickNow = Environment.TickCount;
        float timeDelta = (tickNow - lastTick) / 1000f;
        Vector3 distanceVector = timeDelta * Npc.Velocity.Rotate(Npc.Rotation);
        Npc.Position += distanceVector;

        lastTick = tickNow;
        return base.Sync();
    }

    public override void OnCompleted() {
        if (!Completed) {
            Npc.Velocity = default;
        }
        base.OnCompleted();
    }
}
