using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Core.Data; 

public class ItemCoupleInfo : IByteSerializable {
    public long CharacterId { get; private set; }
    public string Name { get; private set; }

    public ItemCoupleInfo() {
        CharacterId = 0;
        Name = string.Empty;
    }

    public ItemCoupleInfo(long characterId, string name) {
        this.CharacterId = characterId;
        this.Name = name;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong(CharacterId);
        if (CharacterId != 0) {
            writer.WriteUnicodeString(Name);
        }
    }

    public void ReadFrom(IByteReader reader) {
        CharacterId = reader.ReadLong();
        if (CharacterId != 0) {
            Name = reader.ReadUnicodeString();
        }
    }
}