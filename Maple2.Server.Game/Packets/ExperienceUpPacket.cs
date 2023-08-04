using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class ExperienceUpPacket {
    private enum Command : byte {
        Add = 0,
        SetRestExp = 1,
    }

    public static ByteWriter Add(long gainedExp, long totalExp, long restExp, int sourceObjectId, bool additional = false) {
        var pWriter = Packet.Of(SendOp.ExpUp);
        pWriter.Write<Command>(Command.Add);
        pWriter.WriteLong(gainedExp);
        pWriter.WriteShort(); // Unknown
        pWriter.WriteLong(totalExp);
        pWriter.WriteLong(restExp);
        pWriter.WriteInt(sourceObjectId);
        pWriter.WriteBool(additional);

        return pWriter;
    }

    public static ByteWriter Add(long gainedExp, long totalExp, long restExp, ExpMessageCode expMessageCode, bool additional = false) {
        var pWriter = Packet.Of(SendOp.ExpUp);
        pWriter.Write<Command>(Command.Add);
        pWriter.WriteLong(gainedExp);
        pWriter.WriteShort(); // Unknown
        pWriter.WriteLong(totalExp);
        pWriter.WriteLong(restExp);
        pWriter.Write<ExpMessageCode>(expMessageCode);
        pWriter.WriteBool(additional);

        return pWriter;
    }

    public static ByteWriter SetRestExp(long restExp) {
        var pWriter = Packet.Of(SendOp.ExpUp);
        pWriter.Write<Command>(Command.SetRestExp);
        pWriter.WriteLong(restExp);

        return pWriter;
    }
}
