using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class PlayerKillNoticePacket {

    public static ByteWriter Notice(int playerObjectId) {
        var pWriter = Packet.Of(SendOp.PlayerKillNotice);
        pWriter.WriteInt(playerObjectId);
        pWriter.WriteByte();
        return pWriter;
    }
}
