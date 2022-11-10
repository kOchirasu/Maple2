using Maple2.PacketLib.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Causes USER_SYNC packets to be set less often (~300ms)
/// </summary>
public class TrafficOptimizer : GameEvent {
    public int GuideObjectSyncInterval;
    public int RideSyncInterval;
    public int LinearMovementInterval;
    public int UserSyncInterval;
    public TrafficOptimizer(int guideObjectSyncInterval, int rideSyncInterval, int linearMovementInterval, int userSyncInterval) : base(nameof(TrafficOptimizer)) {
        GuideObjectSyncInterval = guideObjectSyncInterval;
        RideSyncInterval = rideSyncInterval;
        LinearMovementInterval = linearMovementInterval;
        UserSyncInterval = userSyncInterval;
    }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt(1);
        writer.WriteInt(GuideObjectSyncInterval);
        writer.WriteInt(RideSyncInterval);
        writer.WriteInt(100);
        writer.WriteInt(0);
        writer.WriteInt(LinearMovementInterval);
        writer.WriteInt(UserSyncInterval);
        writer.WriteInt(100);
    }
}
