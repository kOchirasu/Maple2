using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class ItemStats : IByteSerializable, IByteDeserializable {
    public static readonly ItemStats Default = new ItemStats();
    
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

    private readonly IDictionary<StatAttribute, StatOption>[] statOption;
    private readonly IDictionary<SpecialAttribute, SpecialOption>[] specialOption;

    public ItemStats() {
        statOption = new IDictionary<StatAttribute, StatOption>[TYPE_COUNT];
        specialOption = new IDictionary<SpecialAttribute, SpecialOption>[TYPE_COUNT];
        for (int i = 0; i < TYPE_COUNT; i++) {
            statOption[i] = new Dictionary<StatAttribute, StatOption>();
            specialOption[i] = new Dictionary<SpecialAttribute, SpecialOption>();
        }
    }

    public ItemStats(IDictionary<StatAttribute, StatOption>[] statOption, IDictionary<SpecialAttribute, SpecialOption>[] specialOption) {
        this.statOption = statOption;
        this.specialOption = specialOption;
    }

    public ItemStats(ItemStats other) {
        statOption = new IDictionary<StatAttribute, StatOption>[TYPE_COUNT];
        specialOption = new IDictionary<SpecialAttribute, SpecialOption>[TYPE_COUNT];
        for (int i = 0; i < TYPE_COUNT; i++) {
            statOption[i] = new Dictionary<StatAttribute, StatOption>(other.statOption[i]);
            specialOption[i] = new Dictionary<SpecialAttribute, SpecialOption>(other.specialOption[i]);
        }
    }

    public IDictionary<StatAttribute, StatOption> GetStatOptions(Type type) {
        return statOption[(int)type];
    }
    
    public IDictionary<SpecialAttribute, SpecialOption> GetSpecialOptions(Type type) {
        return specialOption[(int)type];
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteByte();
        for (int i = 0; i < TYPE_COUNT; i++) {
            writer.WriteShort((short) statOption[i].Count);
            foreach ((StatAttribute type, StatOption option) in statOption[i]) {
                writer.WriteShort((short)type);
                writer.Write<StatOption>(option);
            }
            writer.WriteShort((short) specialOption[i].Count);
            foreach ((SpecialAttribute type, SpecialOption option) in specialOption[i]) {
                writer.WriteShort((short)type);
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
                var type = (StatAttribute)reader.ReadShort();
                statOption[i][type] = reader.Read<StatOption>();
            }
            short specialCount = reader.ReadShort();
            for (int j = 0; j < specialCount; j++) {
                var type = (SpecialAttribute)reader.ReadShort();
                specialOption[i][type] = reader.Read<SpecialOption>();
            }

            reader.ReadInt();
        }
    }
}
