using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game; 

public class ItemPet : IByteSerializable {
    public string Name;
    public long Exp;
    public int Level;

    public ItemPet() {
        Name = string.Empty;
    }
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteUnicodeString(Name);
        writer.WriteInt();
        writer.WriteLong(Exp);
        writer.WriteInt(Level);
        writer.WriteByte();
    }

    public void ReadFrom(IByteReader reader) {
        Name = reader.ReadUnicodeString();
        reader.ReadInt();
        Exp = reader.ReadLong();
        Level = reader.ReadInt();
        reader.ReadByte();
    }
}
