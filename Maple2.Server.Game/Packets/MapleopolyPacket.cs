using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.Event;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class MapleopolyPacket {
    private enum Command : byte {
        Load = 0,
        Roll = 2,
        Result = 4,
        Error = 6,
    }

    public static ByteWriter Load(IList<BlueMarbleTile> tiles) {
        var pWriter = Packet.Of(SendOp.Mapleopoly);
        pWriter.Write<Command>(Command.Load);
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteInt(tiles.Count);

        foreach (BlueMarbleTile tile in tiles) {
            pWriter.WriteClass(tile);
        }

        return pWriter;
    }
}
