using System.Runtime.InteropServices;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 19)]
public struct SkillCast {
    public long Id;
    public int OwnerId;
    public int SkillId;
    public short SkillLevel;
    public byte MotionPoint;
}
