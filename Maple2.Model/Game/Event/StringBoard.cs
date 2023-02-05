using Maple2.PacketLib.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Displays a string on the top left marquee
/// </summary>
public class StringBoard : GameEventInfo {
    /// <summary>
    ///  If > 0, the string will be pulled from /string/*/stringboardtext.xml. Otherwise, it will use a custom string from the String property.
    /// </summary>
    public int StringId;
    /// <summary>
    /// Custom string to display if StringId is 0.
    /// </summary>
    public string String;

    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteInt(StringId);
        writer.WriteUnicodeString(String);
    }
}
