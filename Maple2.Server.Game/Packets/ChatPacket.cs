using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class ChatPacket {
    public static ByteWriter Message(Player player, ChatType type, string message) {
        var pWriter = Packet.Of(SendOp.USER_CHAT);
        pWriter.WriteLong(player.Account.Id);
        pWriter.WriteLong(player.Character.Id);
        pWriter.WriteUnicodeString(player.Character.Name);
        pWriter.WriteByte();
        pWriter.WriteUnicodeString(message);
        pWriter.Write<ChatType>(type);
        pWriter.WriteByte();
        pWriter.WriteInt();
        pWriter.WriteByte();

        return pWriter;
    }
}
