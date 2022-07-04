using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class UgcItemCube : IByteSerializable, IByteDeserializable {
    public int Id { get; private set; }
    public long Uid { get; private set; }
    public UgcItemLook? Template { get; private set; }

    public UgcItemCube(int id, long uid, UgcItemLook? template) {
        Id = id;
        Uid = uid;
        Template = template;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteLong(Uid);
        writer.WriteLong();

        writer.WriteBool(Template != null);
        if (Template != null) {
            writer.WriteClass<UgcItemLook>(Template);
        }
    }

    public void ReadFrom(IByteReader reader) {
        Id = reader.ReadInt();
        Uid = reader.ReadLong();
        reader.ReadLong();

        bool hasTemplate = reader.ReadBool();
        if (hasTemplate) {
            Template = reader.ReadClass<UgcItemLook>();
        }
    }
}
