using System.Collections.Generic;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class TrophyPacket {
    private enum Command : byte {
        Initialize = 0,
        Load = 1,
        Update = 2,
        Favorite = 4,
    }

    public static ByteWriter Initialize() {
        var pWriter = Packet.Of(SendOp.Achieve);
        pWriter.Write<Command>(Command.Initialize);

        return pWriter;
    }

    public static ByteWriter Load(IList<TrophyEntry> trophies) {
        var pWriter = Packet.Of(SendOp.Achieve);
        pWriter.Write<Command>(Command.Load);
        pWriter.WriteInt(trophies.Count);

        foreach (TrophyEntry trophy in trophies) {
            pWriter.WriteInt(trophy.Id);
            pWriter.WriteInt(1); // Unknown
            pWriter.WriteClass<TrophyEntry>(trophy);
        }

        return pWriter;
    }

    public static ByteWriter Update(TrophyEntry trophy) {
        var pWriter = Packet.Of(SendOp.Achieve);
        pWriter.Write<Command>(Command.Update);
        pWriter.WriteInt(trophy.Id);
        pWriter.WriteClass<TrophyEntry>(trophy);

        return pWriter;
    }

    public static ByteWriter Favorite(TrophyEntry trophy) {
        var pWriter = Packet.Of(SendOp.Achieve);
        pWriter.Write<Command>(Command.Favorite);
        pWriter.WriteInt(trophy.Id);
        pWriter.WriteBool(trophy.Favorite);

        return pWriter;
    }
}
