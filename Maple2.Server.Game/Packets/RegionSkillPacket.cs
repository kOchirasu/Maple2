using System.Numerics;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;

namespace Maple2.Server.Game.Packets;

public class RegionSkillPacket {
    private enum Command : byte {
        Add = 0,
        Remove = 1,
    }

    public static ByteWriter Add(FieldSkillSource skillSource) {
        var pWriter = Packet.Of(SendOp.RegionSkill);
        pWriter.Write<Command>(Command.Add);
        pWriter.WriteInt(skillSource.ObjectId);
        pWriter.WriteInt(skillSource.ObjectId);
        pWriter.WriteInt();
        pWriter.WriteByte(1); // count
        pWriter.Write<Vector3>(skillSource.Position);

        pWriter.WriteInt(skillSource.Value.Id);
        pWriter.WriteShort(skillSource.Value.Level);
        pWriter.WriteFloat(skillSource.Rotation.Z); // RotationH
        pWriter.WriteFloat(); // RotationV / 100

        return pWriter;
    }

    public static ByteWriter Remove(int objectId) {
        var pWriter = Packet.Of(SendOp.RegionSkill);
        pWriter.Write<Command>(Command.Remove);
        pWriter.WriteInt(objectId);

        return pWriter;
    }
}
