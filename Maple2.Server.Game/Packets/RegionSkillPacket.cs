using System.Numerics;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;

namespace Maple2.Server.Game.Packets;

public static class RegionSkillPacket {
    private enum Command : byte {
        Add = 0,
        Remove = 1,
    }

    public static ByteWriter Add(FieldSkill skillSource) {
        var pWriter = Packet.Of(SendOp.RegionSkill);
        pWriter.Write<Command>(Command.Add);
        pWriter.WriteInt(skillSource.ObjectId);
        pWriter.WriteInt(skillSource.Caster.ObjectId);
        pWriter.WriteInt(skillSource.NextTick);
        pWriter.WriteByte((byte) skillSource.Points.Length);
        foreach (Vector3 point in skillSource.Points) {
            pWriter.Write<Vector3>(point);
        }

        pWriter.WriteInt(skillSource.Value.Id);
        pWriter.WriteShort(skillSource.Value.Level);
        pWriter.WriteFloat(skillSource.UseDirection ? skillSource.Rotation.Z : 0); // RotationH
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
