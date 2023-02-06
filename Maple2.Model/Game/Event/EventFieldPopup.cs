using Maple2.PacketLib.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Creates a UI window for the user to quick teleport to an event map.
/// </summary>
public class EventFieldPopup : GameEventInfo {
    public int MapId;

    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteInt(MapId);
    }
}
