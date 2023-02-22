using System.Numerics;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;

namespace Maple2.Server.Game.Model.State;

public sealed class StateJumpNpc : NpcState {
    public readonly bool IsAbsolute;
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

        IsAbsolute = true;
        StartPosition = startPosition;
        EndPosition = endPosition;
        Duration = duration;
        Height = height;
    }

    public StateJumpNpc(in Vector3 endPosition) {
        State = ActorState.Jump;
        SubState = ActorSubState.Jump_Jump; // Not sure, seems to be None

        EndPosition = endPosition;
        Duration = 1f;
        Height = 0.45f;
    }

    public override void WriteTo(IByteWriter writer) {
        writer.WriteBool(IsAbsolute);
        if (IsAbsolute) {
            writer.Write<Vector3>(StartPosition);
            writer.Write<Vector3>(EndPosition);
            writer.WriteFloat(Duration);
            writer.WriteFloat(Height);
        } else {
            writer.Write<Vector3>(EndPosition);
        }
    }
}
