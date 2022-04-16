using System.Collections.Generic;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Core.Data; 

public class ItemEnchant : IByteSerializable {
    public int Enchants { get; private set; }
    public int EnchantExp { get; private set; }
    // Enchant based peachy charges, otherwise always require 10 charges
    public byte EnchantCharges { get; set; } = 1;
    public bool CanRepack { get; private set; }
    public int Charges { get; private set; }

    public readonly IList<StatOption> StatOptions;

    public ItemEnchant() {
        StatOptions = new List<StatOption>();
    }
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Enchants);
        writer.WriteInt(EnchantExp);
        writer.WriteByte(EnchantCharges);
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteBool(CanRepack);
        writer.WriteInt(Charges);
        
        writer.WriteByte((byte)StatOptions.Count);
        foreach (StatOption option in StatOptions) {
            writer.Write<StatOption>(option);
        }
    }

    public void ReadFrom(IByteReader reader) {
        Enchants = reader.ReadInt();
        EnchantExp = reader.ReadInt();
        EnchantCharges = reader.ReadByte();
        reader.ReadLong();
        reader.ReadInt();
        reader.ReadInt();
        CanRepack = reader.ReadBool();
        Charges = reader.ReadInt();

        byte count = reader.ReadByte();
        for (int i = 0; i < count; i++) {
            StatOptions.Add(reader.Read<StatOption>());
        }
    }
}
