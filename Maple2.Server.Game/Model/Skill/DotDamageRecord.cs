using Maple2.Model.Enum;

namespace Maple2.Server.Game.Model.Skill;

public class DotDamageRecord {
    public int OwnerId;
    public int TargetId;
    public int Count;
    public DamageType Type;
    public long Amount;
}
