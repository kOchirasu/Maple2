using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class StateSyncPacket {
    public static void Player(this PoolByteWriter buffer, int playerObjectId, params StateSync[] syncStates) {
        buffer.Write<ushort>(SendOp.USER_SYNC);
        buffer.WriteInt(playerObjectId);
        buffer.WriteByte((byte) syncStates.Length);
        foreach (StateSync entry in syncStates) {
            buffer.WriteClass<StateSync>(entry);
        }
    }

    public static void Ride(this PoolByteWriter buffer, int playerObjectId, params StateSync[] syncStates) {
        buffer.Write<ushort>(SendOp.RIDE_SYNC);
        buffer.WriteInt(playerObjectId);
        buffer.WriteByte((byte) syncStates.Length);
        foreach (StateSync entry in syncStates) {
            buffer.WriteClass<StateSync>(entry);
        }
    }

    public static ByteWriter SyncNumber(byte number) {
        var pWriter = Packet.Of(SendOp.SYNC_NUMBER);
        pWriter.WriteByte(number);

        return pWriter;
    }
}
