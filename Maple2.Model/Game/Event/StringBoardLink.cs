using Maple2.PacketLib.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Enables the ingame marquee to be clicked and open a URL via the in game browser.
/// </summary>
public class StringBoardLink : GameEventInfo {
    public string Url;

    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteUnicodeString(Url);
    }
}
