using Maple2.PacketLib.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Causes USER_SYNC packets to be set less often (~300ms)
/// </summary>
public class TrafficOptimizer : GameEvent {
    public TrafficOptimizer() : base(nameof(TrafficOptimizer)) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt(1);
        writer.WriteInt(300);
        writer.WriteInt(300);
        writer.WriteInt(100);
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteInt(300);
        writer.WriteInt(100);
    }
}
