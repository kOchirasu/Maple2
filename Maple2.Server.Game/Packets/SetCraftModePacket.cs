using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class SetCraftModePacket {
    private enum Command : byte {
        Stop = 0,
        Start = 1,
    }

    public static ByteWriter Stop(int playerObjectId) {
        var pWriter = Packet.Of(SendOp.SetCraftMode);
        pWriter.WriteInt(playerObjectId);
        pWriter.Write<Command>(Command.Stop);

        return pWriter;
    }

    public static ByteWriter Start(int playerObjectId, UgcItemCube cube) {
        var pWriter = Packet.Of(SendOp.SetCraftMode);
        pWriter.WriteInt(playerObjectId);
        pWriter.Write<Command>(Command.Start);
        pWriter.WriteClass<UgcItemCube>(cube);
        pWriter.WriteInt(); // Unknown (amount?)

        return pWriter;
    }
}
