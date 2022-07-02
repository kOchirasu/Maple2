using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class RidePacket {
    private enum Command : byte {
        Start = 0,
        Stop = 1,
        Change = 2,
        StartShared = 3,
        StopShared = 4,
        ChangeShared = 5,
    }

    public static ByteWriter Start(int ownerId, RideOnAction action) {
        var pWriter = Packet.Of(SendOp.ResponseRide);
        pWriter.Write<Command>(Command.Start);
        pWriter.WriteInt(ownerId);
        pWriter.WriteClass<RideOnAction>(action);

        return pWriter;
    }

    public static ByteWriter Stop(int ownerId, RideOffAction action) {
        var pWriter = Packet.Of(SendOp.ResponseRide);
        pWriter.Write<Command>(Command.Stop);
        pWriter.WriteInt(ownerId);
        pWriter.WriteClass<RideOffAction>(action);

        return pWriter;
    }

    public static ByteWriter Change() {
        var pWriter = Packet.Of(SendOp.ResponseRide);
        pWriter.Write<Command>(Command.Change);
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteLong();

        return pWriter;
    }

    public static ByteWriter StartShared() {
        var pWriter = Packet.Of(SendOp.ResponseRide);
        pWriter.Write<Command>(Command.StartShared);
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteByte();

        return pWriter;
    }

    public static ByteWriter StopShared() {
        var pWriter = Packet.Of(SendOp.ResponseRide);
        pWriter.Write<Command>(Command.StopShared);
        pWriter.WriteInt();
        pWriter.WriteInt();

        return pWriter;
    }

    public static ByteWriter ChangeShared() {
        var pWriter = Packet.Of(SendOp.ResponseRide);
        pWriter.Write<Command>(Command.ChangeShared);
        pWriter.WriteInt();

        return pWriter;
    }
}
