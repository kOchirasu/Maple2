using Maple2.Model.Common;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class ItemExtractionPacket {
    private enum Command : byte {
        Extract = 0,
        FullInventory = 1,
        InsufficientAnvils = 2,
    }

    public static ByteWriter Extract(long sourceUid, Item item) {
        var pWriter = Packet.Of(SendOp.GlamourAnvil);
        pWriter.Write<Command>(Command.Extract);
        pWriter.WriteLong(sourceUid);
        pWriter.WriteLong(item.Uid);
        pWriter.WriteShort();
        pWriter.Write<EquipColor>(item.Appearance.Color);
        pWriter.WriteInt();
        pWriter.WriteByte();
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();

        return pWriter;
    }

    public static ByteWriter FullInventory() {
        var pWriter = Packet.Of(SendOp.GlamourAnvil);
        pWriter.Write<Command>(Command.FullInventory);
        return pWriter;
    }

    public static ByteWriter InsufficientAnvils() {
        var pWriter = Packet.Of(SendOp.GlamourAnvil);
        pWriter.Write<Command>(Command.InsufficientAnvils);
        return pWriter;
    }
}
