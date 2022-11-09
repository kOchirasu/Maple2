﻿using System.Runtime.InteropServices;
using Maple2.Model.Enum;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
public readonly struct IngredientInfo {
    public readonly int Unknown;
    public readonly ItemTag Tag;
    public readonly int Amount;

    public IngredientInfo(ItemTag tag, int amount) {
        Unknown = 0;
        Tag = tag;
        Amount = amount;
    }
}
