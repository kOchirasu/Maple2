using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game; 

public class ItemCoupleInfo : IByteSerializable {
    public static readonly ItemCoupleInfo Default = new ItemCoupleInfo();
    
    public long CharacterId { get; private set; }
    public string Name { get; private set; }

    public ItemCoupleInfo(long characterId = 0, string name = "") {
        CharacterId = characterId;
        Name = name;
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