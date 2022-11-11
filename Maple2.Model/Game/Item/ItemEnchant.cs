﻿using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class ItemEnchant : IByteSerializable, IByteDeserializable {
    public static readonly ItemEnchant Default = new ItemEnchant();

    public int Enchants { get; set; }
    public int EnchantExp { get; set; }
    // Enchant based peachy charges, otherwise always require 10 charges
    public byte EnchantCharges { get; set; }
    public bool CanRepack { get; private set; }
    public int Charges { get; set; }

    public readonly Dictionary<StatAttribute, StatOption> StatOptions;

    public ItemEnchant(int enchants = 0, int enchantExp = 0, byte enchantCharges = 1, bool canRepack = true,
            int charges = 0, Dictionary<StatAttribute, StatOption>? statOptions = null) {
        Enchants = enchants;
        EnchantExp = enchantExp;
        EnchantCharges = enchantCharges;
        CanRepack = canRepack;
        Charges = charges;
        StatOptions = statOptions ?? new Dictionary<StatAttribute, StatOption>();
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
        foreach ((StatAttribute type, StatOption option) in StatOptions) {
            writer.WriteInt((int)type);
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
            var type = (StatAttribute)reader.ReadInt();
            StatOptions[type] = reader.Read<StatOption>();
        }
    }
}
