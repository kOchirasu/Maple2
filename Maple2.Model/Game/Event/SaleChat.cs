using Maple2.PacketLib.Tools;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Applies a meret discount to world and/or channel chats
/// </summary>
public class SaleChat : GameEventInfo {
    /// <summary>
    /// Discount applied to world chat
    /// </summary>
    /// <example> 9000 = 90% discount </example>
    public int WorldChatDiscount;
    /// <summary>
    /// Discount applied to channel chat
    /// </summary>
    /// <example> 9000 = 90% discount </example>
    public int ChannelChatDiscount;

    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteInt(WorldChatDiscount);
        writer.WriteInt(ChannelChatDiscount);
    }
}
