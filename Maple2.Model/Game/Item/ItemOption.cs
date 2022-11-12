using System;
using System.Runtime.InteropServices;

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
