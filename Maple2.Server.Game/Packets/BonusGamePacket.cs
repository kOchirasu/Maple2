using System.Collections.Generic;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class BonusGamePacket {
    private enum Command : byte {
        Load = 1,
    }

    public static ByteWriter Load() {
        var pWriter = Packet.Of(SendOp.BonusGame);
        pWriter.Write<Command>(Command.Load);
        pWriter.WriteByte();
        pWriter.WriteInt();
        pWriter.WriteInt();

        return pWriter;
    }
}
