using Maple2.Model.Enum;

namespace Maple2.Model.Game;

public class DotDamageRecord {
    public int OwnerId;
    public int TargetId;
    public int Count;
    public DamageType Type;
    public long Amount;
}
