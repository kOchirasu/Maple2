using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public sealed class UgcItemLook : IByteSerializable, IByteDeserializable {
    public static readonly UgcItemLook Default = new UgcItemLook();

    public string FileName;
    public string Name;
    public long AccountId;
    public long CharacterId;
    public string Author;
    public long CreationTime;
    public string Url;

    public UgcItemLook() {
        FileName = string.Empty;
        Name = string.Empty;
        Author = string.Empty;
        Url = string.Empty;
    }

    public UgcItemLook Clone() {
        return (UgcItemLook) MemberwiseClone();
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong();
        writer.WriteUnicodeString(FileName); // UUID (filename)
        writer.WriteUnicodeString(Name); // Name (itemname)
        writer.WriteByte();
        writer.WriteInt();
        writer.WriteLong(AccountId); // AccountId
        writer.WriteLong(CharacterId); // CharacterId
        writer.WriteUnicodeString(Author); // CharacterName
        writer.WriteLong(CreationTime); // CreationTime
        writer.WriteUnicodeString(Url); // URL (no domain)
        writer.WriteByte();
    }

    public void ReadFrom(IByteReader reader) {
        reader.ReadLong();
        FileName = reader.ReadUnicodeString();
        Name = reader.ReadUnicodeString();
        reader.ReadByte();
        reader.ReadInt();
        AccountId = reader.ReadLong();
        CharacterId = reader.ReadLong();
        Author = reader.ReadUnicodeString();
        CreationTime = reader.ReadLong();
        Url = reader.ReadUnicodeString();
        reader.ReadByte();
    }
}
