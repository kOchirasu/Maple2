using Maple2.PacketLib.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Causes USER_SYNC packets to be set less often (~300ms)
/// </summary>
public class TrafficOptimizer : GameEventInfo {
    public int GuideObjectSyncInterval;
    public int RideSyncInterval;
    public int LinearMovementInterval;
    public int UserSyncInterval;
    
    public TrafficOptimizer() : base(nameof(TrafficOptimizer)) { }

    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteInt(GuideObjectSyncInterval);
        writer.WriteInt(RideSyncInterval);
        writer.WriteInt(100);
        writer.WriteInt(0);
        writer.WriteInt(LinearMovementInterval);
        writer.WriteInt(UserSyncInterval);
        writer.WriteInt(100);
    }
}
