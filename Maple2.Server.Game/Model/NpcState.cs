using System.Numerics;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Game.Model;

public class NpcState : IByteSerializable {
    public ActorState State { get; init; } = ActorState.Idle;
    public virtual ActorSubState SubState { get; init; } = ActorSubState.Idle_Idle;

    public virtual void WriteTo(IByteWriter writer) { }
}

public sealed class NpcStateJump : NpcState {
    public readonly Vector3 StartPosition;
    public readonly Vector3 EndPosition;
    public readonly float Duration;
    public readonly float Height;

    public override ActorSubState SubState {
        get => base.SubState;
        init {
            base.SubState = value;
            if (base.SubState.State() is not (ActorState.Jump or ActorState.JumpTo)) {
                base.SubState = ActorSubState.Jump_Jump;
            }
            State = base.SubState.State();
        }
    }

    public NpcStateJump(in Vector3 startPosition, in Vector3 endPosition, float duration, float height) {
        StartPosition = startPosition;
        EndPosition = endPosition;
        Duration = duration;
        Height = height;
    }

    // Also has a relative format
    // endPosition
    // duration = 1
    // height = 0.45
    public override void WriteTo(IByteWriter writer) {
        writer.WriteBool(true);
        writer.Write<Vector3>(StartPosition);
        writer.Write<Vector3>(EndPosition);
        writer.WriteFloat(Duration);
        writer.WriteFloat(Height);
    }
}

public sealed class NpcStateHit : NpcState {
    public readonly float UnknownF1;
    public readonly float UnknownF2;
    public readonly float UnknownF3;
    public readonly byte UnknownB;

    public NpcStateHit(float unknownF1, float unknownF2, float unknownF3, byte unknownB) {
        State = ActorState.Hit;
        SubState = ActorSubState.None;

        UnknownF1 = unknownF1;
        UnknownF2 = unknownF2;
        UnknownF3 = unknownF3;
        UnknownB = unknownB;
    }

    public override void WriteTo(IByteWriter writer) {
        writer.WriteFloat(UnknownF1);
        writer.WriteFloat(UnknownF2);
        writer.WriteFloat(UnknownF3);
        writer.WriteByte(UnknownB);
    }
}

public sealed class NpcStateSpawn : NpcState {
    public readonly int Unknown;

    public NpcStateSpawn(int unknown) {
        State = ActorState.Spawn;
        SubState = ActorSubState.None;

        Unknown = unknown;
    }

    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Unknown);
    }
}
