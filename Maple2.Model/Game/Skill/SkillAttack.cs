using System.Runtime.InteropServices;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 20)]
public struct SkillAttack {
    public long Id;
    public int OwnerId;
    public int SkillId;
    public short SkillLevel;
    public byte MotionPoint;
    public byte AttackPoint;
}
