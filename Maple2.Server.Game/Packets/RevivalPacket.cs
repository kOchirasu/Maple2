using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;

namespace Maple2.Server.Game.Packets;

public static class RevivalPacket {
    public static ByteWriter Confirm(IActor<Player> player, int ticks = 0, int counter = 0) {
        var pWriter = Packet.Of(SendOp.REVIVAL_CONFIRM);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteInt(ticks); // 0 in house
        pWriter.WriteInt(counter); // (some counter); 0 in house

        return pWriter;
    }

    public static ByteWriter Count(int count) {
        var pWriter = Packet.Of(SendOp.REVIVAL_COUNT);
        pWriter.WriteInt(count);

        return pWriter;
    }
}
