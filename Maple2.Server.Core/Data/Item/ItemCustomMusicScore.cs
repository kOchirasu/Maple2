using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Core.Data; 

public class ItemCustomMusicScore : IByteSerializable {
    public int Length;
    public int Instrument;
    public string Title;
    public string Author;
    public long CharacterId; // Of Author
    public bool IsLocked;

    public ItemCustomMusicScore() {
        Title = string.Empty;
        Author = string.Empty;
    }
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Length);
        writer.WriteInt(Instrument);
        writer.WriteUnicodeString(Title);
        writer.WriteUnicodeString(Author);
        writer.WriteInt(1);
        writer.WriteLong(CharacterId);
        writer.WriteBool(IsLocked);
        writer.WriteLong();
        writer.WriteLong();
    }

    public void ReadFrom(IByteReader reader) {
        Length = reader.ReadInt();
        Instrument = reader.ReadInt();
        Title = reader.ReadUnicodeString();
        Author = reader.ReadUnicodeString();
        reader.ReadInt();
        CharacterId = reader.ReadLong();
        IsLocked = reader.ReadBool();
        reader.ReadLong();
        reader.ReadLong();
    }
}
