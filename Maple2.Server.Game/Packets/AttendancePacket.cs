using Maple2.Model.Error;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class AttendancePacket {
    private enum Command : byte {
        Error = 9,
    }

    public static ByteWriter Error(AttendanceError error) {
        var pWriter = Packet.Of(SendOp.Attendance);
        pWriter.Write<Command>(Command.Error);
        pWriter.Write<AttendanceError>(error);

        return pWriter;
    }
}
