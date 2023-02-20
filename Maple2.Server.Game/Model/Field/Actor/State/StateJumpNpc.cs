using System.Numerics;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Model.State;

namespace Maple2.Server.Game.Model;

public sealed class StateJumpNpc : NpcState {
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

    public StateJumpNpc(in Vector3 startPosition, in Vector3 endPosition, float duration, float height) {
        State = ActorState.Jump;
        SubState = ActorSubState.Jump_Jump; // Not sure, seems to be None

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
