using System.Runtime.CompilerServices;
using Maple2.PacketLib.Tools;

namespace Maple2.Server.Core.Packets;

public static class Packet {
    public const int DEFAULT_SIZE = 512;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteWriter Of(ushort opcode, int size = DEFAULT_SIZE) {
        var packet = new ByteWriter(size);
        packet.Write<ushort>(opcode);
        return packet;
    }
}
