using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game; 

public class ItemBlueprint : IByteSerializable, IByteDeserializable {
    public void WriteTo(IByteWriter writer) {
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteUnicodeString();
    }

    public void ReadFrom(IByteReader reader) {
        reader.ReadLong();
        reader.ReadInt();
        reader.ReadInt();
        reader.ReadInt();
        reader.ReadLong();
        reader.ReadInt();
        reader.ReadLong();
        reader.ReadLong();
        reader.ReadUnicodeString();
    }
}
