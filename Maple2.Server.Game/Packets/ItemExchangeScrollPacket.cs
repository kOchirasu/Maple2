using Maple2.Model.Error;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class ItemExchangeScrollPacket {
    private enum Command : byte {
        Mode0 = 0,
        Error = 2
    }
    
    public static ByteWriter Mode0() {
        var pWriter = Packet.Of(SendOp.ItemExchange);
        pWriter.Write<Command>(Command.Mode0);
        pWriter.WriteLong();
        pWriter.WriteLong();
        bool unk = true;
        pWriter.WriteBool(unk);
        if (unk) {
            pWriter.WriteInt();
            pWriter.WriteInt();
            pWriter.WriteInt();
        }

        return pWriter;
    }

    public static ByteWriter Error(ItemExchangeScrollError error) {
        var pWriter = Packet.Of(SendOp.ItemExchange);
        pWriter.Write<Command>(Command.Error);
        pWriter.Write<ItemExchangeScrollError>(error);

        return pWriter;
    }
}
