using Maple2.PacketLib.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Overrides the Meret Market marquee with a custom message.
/// </summary>
public class MeratMarketNotice : GameEventInfo {
    public string Message;

    public override void WriteTo(IByteWriter writer) {
        writer.WriteUnicodeString(Message);
    }
}
