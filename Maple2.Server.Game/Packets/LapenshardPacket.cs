using System.Collections.Generic;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class LapenshardPacket {
    private enum Command : byte {
        Load = 0,
        Equip = 1,
        Unequip = 2,
        Preview = 4,
        Upgrade = 5,
    }

    public static ByteWriter Load(ICollection<Item> items) {
        var pWriter = Packet.Of(SendOp.Lapenshard);
        pWriter.Write<Command>(Command.Load);
        pWriter.WriteInt(items.Count);
        foreach (Item lapenshard in items) {
            pWriter.WriteInt(lapenshard.Slot);
            pWriter.WriteInt(lapenshard.Id);
        }

        return pWriter;
    }

    public static ByteWriter Equip(Item lapenshard) {
        var pWriter = Packet.Of(SendOp.Lapenshard);
        pWriter.Write<Command>(Command.Equip);
        pWriter.WriteInt(lapenshard.Slot);
        pWriter.WriteInt(lapenshard.Id);

        return pWriter;
    }

    public static ByteWriter Unequip(short slot) {
        var pWriter = Packet.Of(SendOp.Lapenshard);
        pWriter.Write<Command>(Command.Unequip);
        pWriter.WriteInt(slot);

        return pWriter;
    }

    public static ByteWriter Preview() {
        var pWriter = Packet.Of(SendOp.Lapenshard);
        pWriter.Write<Command>(Command.Preview);
        pWriter.WriteInt(10000); // div 100 = Rate

        return pWriter;
    }

    public static ByteWriter Upgrade(Item lapenshard, bool success = true) {
        var pWriter = Packet.Of(SendOp.Lapenshard);
        pWriter.Write<Command>(Command.Upgrade);
        pWriter.WriteLong(lapenshard.Uid);
        pWriter.WriteInt(lapenshard.Id);
        pWriter.WriteInt(lapenshard.Rarity);
        pWriter.WriteBool(success);

        return pWriter;
    }
}
