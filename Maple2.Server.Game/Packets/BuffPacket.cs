using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class BuffPacket {
    private enum Command : byte {
        Add = 0,
        Remove = 1,
        Update = 2,
    }

    public static ByteWriter Add(Buff buff) {
        var pWriter = Packet.Of(SendOp.Buff);
        pWriter.Write<Command>(Command.Add);
        pWriter.WriteInt(buff.Target.ObjectId);
        pWriter.WriteInt(buff.ObjectId);
        pWriter.WriteInt(buff.Caster.ObjectId);
        pWriter.WriteClass<Buff>(buff);

        return pWriter;
    }

    public static ByteWriter Remove(Buff buff) {
        var pWriter = Packet.Of(SendOp.Buff);
        pWriter.Write<Command>(Command.Remove);
        pWriter.WriteInt(buff.Target.ObjectId);
        pWriter.WriteInt(buff.ObjectId);
        pWriter.WriteInt(buff.Caster.ObjectId);

        return pWriter;
    }

    public static ByteWriter Update(Buff buff) {
        var pWriter = Packet.Of(SendOp.Buff);
        pWriter.Write<Command>(Command.Update);
        pWriter.WriteInt(buff.Target.ObjectId);
        pWriter.WriteInt(buff.ObjectId);
        pWriter.WriteInt(buff.Caster.ObjectId);
        pWriter.WriteInt(1); // TODO: complete this...
        buff.WriteAdditionalEffect(pWriter);
        // buff.WriteAdditionalEffect2(pWriter);


        return pWriter;
    }
}
