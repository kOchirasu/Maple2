using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game; 

public class ItemLimitBreak : IByteSerializable, IByteDeserializable {
    public static readonly ItemLimitBreak Default = new ItemLimitBreak();
    
    public int Level { get; private set; }
    public readonly IDictionary<StatAttribute, StatOption> StatOptions;
    public readonly IDictionary<SpecialAttribute, SpecialOption> SpecialOptions;

    public ItemLimitBreak() {
        StatOptions = new Dictionary<StatAttribute, StatOption>();
        SpecialOptions = new Dictionary<SpecialAttribute, SpecialOption>();
    }
    
    public ItemLimitBreak(int level, IDictionary<StatAttribute, StatOption> statOptions,
            IDictionary<SpecialAttribute, SpecialOption> specialOptions) {
        Level = level;
        StatOptions = statOptions;
        SpecialOptions = specialOptions;
    }
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Level);
        
        writer.WriteInt(StatOptions.Count);
        foreach ((StatAttribute type, StatOption option) in StatOptions) {
            writer.WriteShort((short)type);
            writer.Write<StatOption>(option);
        }
        writer.WriteInt(SpecialOptions.Count);
        foreach ((SpecialAttribute type, SpecialOption option) in SpecialOptions) {
            writer.WriteShort((short)type);
            writer.Write<SpecialOption>(option);
        }
    }

    public void ReadFrom(IByteReader reader) {
        Level = reader.ReadInt();
        
        int statCount = reader.ReadInt();
        for (int i = 0; i < statCount; i++) {
            var type = (StatAttribute)reader.ReadShort();
            StatOptions[type] = reader.Read<StatOption>();
        }
        int specialCount = reader.ReadInt();
        for (int i = 0; i < specialCount; i++) {
            var type = (SpecialAttribute)reader.ReadShort();
            SpecialOptions[type] = reader.Read<SpecialOption>();
        }
    }
}
