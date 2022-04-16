using System;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Core.Data; 

public class ItemTransfer : IByteSerializable {
    public int Flag { get; private set; }
    public int RemainTrades { get; private set; }
    public int RemainRepackage { get; private set; }
    
    public ItemBinding? Binding { get; private set; }

    public ItemTransfer(int flag, ItemBinding? binding = null) {
        Flag = flag;
        Binding = binding;
    }

    public void WriteTo(IByteWriter writer) {
        // CItemTransfer is CItem[66]
        writer.WriteInt(Flag); // CItemTransfer[5]
        writer.WriteBool(false); // CItemTransfer[9] *bit-1*
        writer.WriteInt(RemainTrades); // CItemTransfer[10]
        writer.WriteInt(RemainRepackage); // CItemTransfer[11]
        writer.WriteByte(); // CItemTransfer[12]
        writer.WriteBool(true); // CItemTransfer[9] *bit-10* (socketTransfer?)

        // CharBound means untradable, unsellable, bound to char (ignores TransferFlag)
        writer.WriteBool(Binding != null);
        if (Binding != null) {
            writer.WriteClass<ItemBinding>(Binding);
        }
    }

    public void ReadFrom(IByteReader reader) {
        Flag = reader.ReadInt();
        reader.ReadByte();
        RemainTrades = reader.ReadInt();
        RemainRepackage = reader.ReadInt();
        reader.ReadByte();
        reader.ReadBool();
        bool isBound = reader.ReadBool();
        if (isBound) {
            Binding = reader.ReadClass<ItemBinding>();
        }
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (!(obj is ItemTransfer other)) return false;
        return Flag == other.Flag && Equals(Binding, other.Binding);
    }

    public override int GetHashCode() {
        return HashCode.Combine((int) Flag, Binding);
    }
}
