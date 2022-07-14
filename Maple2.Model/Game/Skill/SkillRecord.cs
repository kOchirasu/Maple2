using System.Numerics;
using System.Runtime.InteropServices;
using Maple2.Model.Enum;

namespace Maple2.Model.Game;

public class SkillRecord {
    public long Uid;
    public int ServerTick;
    public int CasterId;
    public int SkillId;
    public short Level;
    public byte MotionPoint;
    public byte AttackPoint;

    public Vector3 Position;
    public Vector3 Direction;
    public Vector3 Rotation;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14)]
public readonly record struct TargetRecord(int Counter, int CasterId, int TargetId, ActorState State, ActorSubState SubState);
