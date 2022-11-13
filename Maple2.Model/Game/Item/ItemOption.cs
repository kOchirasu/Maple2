using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Maple2.Model.Enum;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly record struct StatOption(int Value, float Rate = 0) {
    public StatOption(float percent) : this(0, percent) { }

    public static StatOption operator +(StatOption self, StatOption other) {
        return new StatOption(self.Value + other.Value, self.Rate + other.Rate);
    }

    public static StatOption operator -(StatOption self, StatOption other) {
        return new StatOption(Math.Max(self.Value - other.Value, 0), Math.Max(self.Rate - other.Rate, 0));
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly record struct SpecialOption(float Rate, float Value = 0) {
    public static SpecialOption operator +(SpecialOption self, SpecialOption other) {
        return new SpecialOption(self.Rate + other.Rate, self.Value + other.Value);
    }

    public static SpecialOption operator -(SpecialOption self, SpecialOption other) {
        return new SpecialOption(Math.Max(self.Rate - other.Rate, 0), Math.Max(self.Value - other.Value, 0));
    }
}

public readonly struct LockOption {
    private readonly StatAttribute? basic;
    private readonly SpecialAttribute? special;
    private readonly bool lockValue;

    public LockOption(StatAttribute attribute, bool lockValue = false) {
        basic = attribute;
        this.lockValue = lockValue;
    }

    public LockOption(SpecialAttribute attribute, bool lockValue = false) {
        special = attribute;
        this.lockValue = lockValue;
    }

    public bool TryGet(out StatAttribute attribute, out bool valueLocked) {
        valueLocked = lockValue;
        if (basic == null) {
            attribute = 0;
            return false;
        }

        attribute = (StatAttribute) basic;
        return true;
    }

    public bool TryGet(out SpecialAttribute attribute, out bool valueLocked) {
        valueLocked = lockValue;
        if (special == null) {
            attribute = 0;
            return false;
        }

        attribute = (SpecialAttribute) special;
        return true;
    }
}
