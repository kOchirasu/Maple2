using System;
using Maple2.PacketLib.Tools;
using Maple2.Server.Constants;

namespace Maple2.Server.Core.Packets;

// Whenever server sends 02, client makes a request
//
// 01 and 03 seem to be the first ones sent after entering game
// perhaps setting some initial state?
public static class TimeSyncPacket {
    public static ByteWriter Response(int key) {
        var pWriter = Packet.Of(SendOp.RESPONSE_TIME_SYNC);
        pWriter.WriteByte(0x00);
        pWriter.WriteInt(Environment.TickCount);
        pWriter.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        pWriter.WriteByte();
        pWriter.WriteInt();
        pWriter.WriteInt(key);

        return pWriter;
    }

    public static ByteWriter SetInitial1() {
        var pWriter = Packet.Of(SendOp.RESPONSE_TIME_SYNC);
        pWriter.WriteByte(0x01); // 1 and 2
        pWriter.WriteInt(Environment.TickCount);
        pWriter.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        pWriter.WriteByte();
        pWriter.WriteInt();

        return pWriter;
    }

    // Request client to make a request
    public static ByteWriter Request() {
        var pWriter = Packet.Of(SendOp.RESPONSE_TIME_SYNC);
        pWriter.WriteByte(0x02); // 1 and 2
        pWriter.WriteInt(Environment.TickCount);
        pWriter.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        pWriter.WriteByte();
        pWriter.WriteInt();

        return pWriter;
    }

    public static ByteWriter SetInitial2() {
        var pWriter = Packet.Of(SendOp.RESPONSE_TIME_SYNC);
        pWriter.WriteByte(0x03);
        pWriter.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        return pWriter;
    }
}
