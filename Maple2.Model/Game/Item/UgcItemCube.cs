using Maple2.Model.Common;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class UgcItemCube : IByteSerializable, IByteDeserializable {
    public static readonly UgcItemCube Default = new UgcItemCube(0, 0);

    public long Id { get; private set; }
    public int ItemId { get; private set; }
    public UgcItemLook? Template { get; private set; }

    public Vector3B Position { get; set; }
    public float Rotation { get; set; }

    public UgcItemCube(long id, int itemId, UgcItemLook? template = null) {
        ItemId = itemId;
        Id = id;
        Template = template;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(ItemId);
        writer.WriteLong(Id);
        writer.WriteLong();

        writer.WriteBool(Template != null);
        if (Template != null) {
            writer.WriteClass<UgcItemLook>(Template);
        }
    }

    public void ReadFrom(IByteReader reader) {
        ItemId = reader.ReadInt();
        Id = reader.ReadLong();
        reader.ReadLong();

        bool hasTemplate = reader.ReadBool();
        if (hasTemplate) {
            Template = reader.ReadClass<UgcItemLook>();
        }
    }
}
