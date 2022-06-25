using System.Runtime.CompilerServices;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

namespace Maple2.Server.Core.Packets;

public static class Packet {
    public const int DEFAULT_SIZE = 128;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteWriter Of(SendOp opcode, int size = DEFAULT_SIZE) {
        var packet = new ByteWriter(size);
        packet.Write<SendOp>(opcode);
        return packet;
    }
}
