using System.Collections.Generic;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game; 

public class ItemLimitBreak : IByteSerializable {
    public static readonly ItemLimitBreak Default = new ItemLimitBreak();
    
    public int Level { get; private set; }
    public readonly IList<StatOption> StatOptions;
    public readonly IList<SpecialOption> SpecialOptions;

    public ItemLimitBreak() {
        StatOptions = new List<StatOption>();
        SpecialOptions = new List<SpecialOption>();
    }
    
    public ItemLimitBreak(int level, IList<StatOption> statOptions, IList<SpecialOption> specialOptions) {
        Level = level;
        StatOptions = statOptions;
        SpecialOptions = specialOptions;
    }
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Level);
        
        writer.WriteInt(StatOptions.Count);
        foreach (StatOption option in StatOptions) {
            writer.Write<StatOption>(option);
        }
        writer.WriteInt(SpecialOptions.Count);
        foreach (SpecialOption option in SpecialOptions) {
            writer.Write<SpecialOption>(option);
        }
    }

    public void ReadFrom(IByteReader reader) {
        Level = reader.ReadInt();
        
        int statCount = reader.ReadInt();
        for (int i = 0; i < statCount; i++) {
            StatOptions.Add(reader.Read<StatOption>());
        }
        int specialCount = reader.ReadInt();
        for (int i = 0; i < specialCount; i++) {
            SpecialOptions.Add(reader.Read<SpecialOption>());
        }
    }
}
