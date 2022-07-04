using System.Numerics;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Game.Model;

public class Buff : IFieldObject, IByteSerializable {
    public int ObjectId { get; init; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public readonly int Id;
    public readonly IActor Owner;
    public readonly IActor Target;

    public Buff(int objectId, int id, IActor owner, IActor target) {
        ObjectId = objectId;
        Id = id;
        Owner = owner;
        Target = target;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Target.ObjectId);
        writer.WriteInt(ObjectId);
        writer.WriteInt(Owner.ObjectId);
        WriteAdditionalEffect(writer, this);
        writer.WriteLong(); // Unknown, AdditionalEffect2
    }

    private void WriteAdditionalEffect(IByteWriter pWriter, Buff buff) {
        pWriter.WriteInt(0);
        pWriter.WriteInt(0);
        pWriter.WriteInt(buff.Id);
        pWriter.WriteShort(1);
        pWriter.WriteInt(1);
        pWriter.WriteBool(false);
    }
}
