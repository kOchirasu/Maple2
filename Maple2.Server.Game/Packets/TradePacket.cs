using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class TradePacket {
    private enum Command : byte {
        Request = 0,
        Error = 1,
        Acknowledge = 2,
        Decline = 4,
        StartTrade = 5,
        EndTrade = 6,
        AddItem = 8,
        RemoveItem = 9,
        SetMesos = 10,
        Finalize = 11,
        UnFinalize = 12,
        Complete = 13,
        AlreadyRequest = 14,
    }

    public static ByteWriter Request(Player player) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.Request);
        pWriter.WriteUnicodeString(player.Character.Name);
        pWriter.WriteLong(player.Character.Id);

        return pWriter;
    }

    public static ByteWriter Error() {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.Error);
        pWriter.WriteByte();
        pWriter.WriteUnicodeString();
        pWriter.WriteInt();
        pWriter.WriteInt();

        return pWriter;
    }

    public static ByteWriter Acknowledge() {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.Acknowledge);

        return pWriter;
    }

    public static ByteWriter Decline(string name) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.Decline);
        pWriter.WriteUnicodeString(name);

        return pWriter;
    }

    public static ByteWriter StartTrade(long characterId) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.StartTrade);
        pWriter.WriteLong(characterId);

        return pWriter;
    }

    public static ByteWriter EndTrade(bool success) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.EndTrade);
        pWriter.WriteBool(success);

        return pWriter;
    }

    public static ByteWriter AddItem(bool index, Item item) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.AddItem);
        pWriter.WriteBool(index);
        pWriter.WriteInt(item.Id);
        pWriter.WriteLong(item.Uid);
        pWriter.WriteInt(item.Rarity);
        pWriter.WriteInt(item.Slot);
        pWriter.WriteInt(item.Amount);
        pWriter.WriteInt(item.Slot);
        pWriter.WriteClass<Item>(item);

        return pWriter;
    }

    public static ByteWriter RemoveItem(bool index, int tradeSlot, long itemUid) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.RemoveItem);
        pWriter.WriteBool(index);
        pWriter.WriteInt(tradeSlot);
        pWriter.WriteLong(itemUid);

        return pWriter;
    }

    public static ByteWriter SetMesos(bool index, long mesos) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.SetMesos);
        pWriter.WriteBool(index);
        pWriter.WriteLong(mesos);

        return pWriter;
    }

    public static ByteWriter Finalize(bool index) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.Finalize);
        pWriter.WriteBool(index);

        return pWriter;
    }

    public static ByteWriter UnFinalize(bool index) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.UnFinalize);
        pWriter.WriteBool(index);

        return pWriter;
    }

    public static ByteWriter Complete(bool index) {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.Complete);
        pWriter.WriteBool(index);

        return pWriter;
    }

    public static ByteWriter AlreadyRequest() {
        var pWriter = Packet.Of(SendOp.Trade);
        pWriter.Write<Command>(Command.AlreadyRequest);

        return pWriter;
    }
}
