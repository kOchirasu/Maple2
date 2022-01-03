using Maple2.PacketLib.Tools;
using Maple2.Server.Constants;

namespace Maple2.Server.Core.Packets;

public static class RequestPacket {
    public static ByteWriter Login() {
        return Packet.Of(SendOp.REQUEST_LOGIN);
    }

    public static ByteWriter Key() {
        return Packet.Of(SendOp.REQUEST_KEY);
    }

    public static ByteWriter Heartbeat(int key) {
        var pWriter = Packet.Of(SendOp.REQUEST_HEARTBEAT);
        pWriter.WriteInt(key);

        return pWriter;
    }

    public static ByteWriter TickSync(int serverTick) {
        var pWriter = Packet.Of(SendOp.REQUEST_CLIENTTICK_SYNC);
        pWriter.WriteInt(serverTick);

        return pWriter;
    }
}
