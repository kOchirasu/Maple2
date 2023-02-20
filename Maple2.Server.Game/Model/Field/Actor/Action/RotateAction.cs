using System;
using System.Numerics;

namespace Maple2.Server.Game.Model.Action;

public class RotateAction : NpcAction {
    public RotateAction(FieldNpc npc, Vector3 rotation)
            : base(npc, npc.IdleSequence.Id, Math.Abs(rotation.Z) / npc.Value.Metadata.Action.RotateSpeed) {
        npc.Rotation += rotation;
    }

    public RotateAction(FieldNpc npc) : this(npc, Random.Shared.Next(-90, 90) * Vector3.UnitZ) { }

    public override void OnCompleted() {
        // TODO: Handle cancel mid-rotation
    }
}
