using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class FishingPacket {
    private enum Command : byte {
        Prepare = 0,
        Stop = 1,
        Error = 2,
        IncreaseMastery = 3,
        LoadTiles = 4,
        CatchItem = 5,
        LoadAlbum = 7,
        CatchFish = 8,
        Start = 9,
    }
    
    public static ByteWriter Prepare(long rodUid) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.Prepare);
        pWriter.WriteLong(rodUid);

        return pWriter;
    }
    
    public static ByteWriter Error(FishingError error) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.Error);
        pWriter.Write<FishingError>(error);

        return pWriter;
    }

    public static ByteWriter LoadTiles(IList<Vector3> tiles) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.LoadTiles);
        pWriter.WriteByte();
        pWriter.WriteInt(tiles.Count);
        foreach (Vector3 tile in tiles) {
            pWriter.Write<Vector3B>(tile);
            pWriter.WriteInt(10000001);
            pWriter.WriteInt(25);
            pWriter.WriteInt(15000); // fishing time
            pWriter.WriteShort(1);
        }

        return pWriter;
    }
}
