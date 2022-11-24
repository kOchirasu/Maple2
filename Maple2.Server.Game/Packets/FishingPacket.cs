using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class FishingPacket {
    private enum Command : byte {
        Prepare = 0,
        Stop = 1,
        Error = 2,
        IncreaseMastery = 3,
        LoadTiles = 4,
        CatchItem = 5,
        PrizeFish = 6,
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
    
    public static ByteWriter Stop() {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.Stop);

        return pWriter;
    }
    
    public static ByteWriter Error(FishingError error) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.Error);
        pWriter.Write<FishingError>(error);

        return pWriter;
    }
    
    public static ByteWriter IncreaseMastery(FishCatch fishCatch) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.IncreaseMastery);
        pWriter.Write<FishCatch>(fishCatch);

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
            pWriter.WriteInt(15000); // fishing time minus any rod or buff time reduction
            pWriter.WriteShort(1);
        }

        return pWriter;
    }
    
    public static ByteWriter CatchItem(IList<FishingRewardTable.Entry> rewards) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.CatchItem);
        pWriter.WriteInt(rewards.Count);
        foreach (FishingRewardTable.Entry item in rewards) {
            pWriter.WriteInt(item.Id);
            pWriter.WriteInt(item.Amount);
        }

        return pWriter;
    }
    
    public static ByteWriter PrizeFish(string playerName, int fishId) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.PrizeFish);
        pWriter.WriteUnicodeString(playerName);
        pWriter.WriteInt(fishId);
        pWriter.WriteInt();

        return pWriter;
    }
    
    public static ByteWriter LoadAlbum(IDictionary<int, FishEntry> fishAlbum) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.LoadAlbum);
        pWriter.WriteInt(fishAlbum.Count);
        foreach ((int id, FishEntry fish) in fishAlbum) {
            pWriter.WriteClass(fish);
        }

        return pWriter;
    }
    
    public static ByteWriter CatchFish(int id, int size, FishEntry? fish = null) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.CatchFish);
        pWriter.WriteInt(id);
        pWriter.WriteInt(size);
        pWriter.WriteBool(fish != null);
        pWriter.WriteByte();

        if (fish != null) {
            pWriter.WriteClass(fish);
        }

        return pWriter;
    }
    
    public static ByteWriter Start(int fishingTick, bool miniGame) {
        var pWriter = Packet.Of(SendOp.Fishing);
        pWriter.Write<Command>(Command.Start);
        pWriter.WriteBool(miniGame);
        pWriter.WriteInt(fishingTick);

        return pWriter;
    }
}
