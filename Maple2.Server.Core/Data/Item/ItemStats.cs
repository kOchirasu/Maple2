using System.Collections.Generic;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Core.Data;

public class ItemStats : IByteSerializable {
    private const int TYPE_COUNT = 9;
    public enum Type {
        Constant = 0,
        Static = 1,
        Random = 2,
        Title = 3,
        Empowerment1= 4,
        Empowerment2= 5,
        Empowerment3= 6,
        Empowerment4= 7,
        Empowerment5= 8,
    }

    private readonly IList<StatOption>[] statOption;
    private readonly IList<SpecialOption>[] specialOption;

    public ItemStats() {
        statOption = new IList<StatOption>[TYPE_COUNT];
        specialOption = new IList<SpecialOption>[TYPE_COUNT];
        for (int i = 0; i < TYPE_COUNT; i++) {
            statOption[i] = new List<StatOption>();
            specialOption[i] = new List<SpecialOption>();
        }
    }

    public ItemStats(ItemStats other) {
        statOption = new IList<StatOption>[TYPE_COUNT];
        specialOption = new IList<SpecialOption>[TYPE_COUNT];
        for (int i = 0; i < TYPE_COUNT; i++) {
            statOption[i] = new List<StatOption>(other.statOption[i]);
            specialOption[i] = new List<SpecialOption>(other.specialOption[i]);
        }
    }

    public IList<StatOption> GetStatOptions(Type type) {
        return statOption[(int)type];
    }
    
    public IList<SpecialOption> GetSpecialOptions(Type type) {
        return specialOption[(int)type];
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteByte();
        for (int i = 0; i < TYPE_COUNT; i++) {
            writer.WriteShort((short) statOption[i].Count);
            foreach (StatOption option in statOption[i]) {
                writer.Write<StatOption>(option);
            }
            writer.WriteShort((short) specialOption[i].Count);
            foreach (SpecialOption option in specialOption[i]) {
                writer.Write<SpecialOption>(option);
            }
            
            writer.WriteInt();
        }
    }

    public void ReadFrom(IByteReader reader) {
        reader.ReadByte();
        for (int i = 0; i < TYPE_COUNT; i++) {
            short statCount = reader.ReadShort();
            for (int j = 0; j < statCount; j++) {
                statOption[i].Add(reader.Read<StatOption>());
            }
            short specialCount = reader.ReadShort();
            for (int j = 0; j < specialCount; j++) {
                specialOption[i].Add(reader.Read<SpecialOption>());
            }

            reader.ReadInt();
        }
    }
}