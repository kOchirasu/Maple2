using System.Numerics;
using System.Runtime.InteropServices;
using Maple2.Model.Enum;

namespace Maple2.Server.Game.Model.Skill;

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
    public float Rotate2Z;
    public bool Unknown;

    public bool IsHold;
    public int HoldInt;
    public string HoldString = string.Empty;

    public override string ToString() {
        return $"Uid:{Uid}, SkillId:{SkillId}, Level:{Level}, MotionPoint:{MotionPoint}, AttackPoint:{AttackPoint}\n"
               + $"- Position:{Position}\n"
               + $"- Rotation:{Rotation}\n"
               + $"- Direction:{Direction}";
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14)]
public readonly record struct TargetRecord(int Counter, int CasterId, int TargetId, ActorState State, ActorSubState SubState);
