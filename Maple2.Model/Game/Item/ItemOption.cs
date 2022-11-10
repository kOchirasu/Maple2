using System;
using System.Runtime.InteropServices;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly record struct StatOption(int Value, float Percent = 0) {
    public StatOption(float percent) : this(0, percent) { }

    public static StatOption operator +(StatOption self, StatOption other) {
        return new StatOption(self.Value + other.Value, self.Percent + other.Percent);
    }

    public static StatOption operator -(StatOption self, StatOption other) {
        return new StatOption(Math.Max(self.Value - other.Value, 0), Math.Max(self.Percent - other.Percent, 0));
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly record struct SpecialOption(float Value, float Unknown = 0) {
    public static SpecialOption operator +(SpecialOption self, SpecialOption other) {
        return new SpecialOption(self.Value + other.Value, self.Unknown + other.Unknown);
    }

    public static SpecialOption operator -(SpecialOption self, SpecialOption other) {
        return new SpecialOption(Math.Max(self.Value - other.Value, 0), Math.Max(self.Unknown - other.Unknown, 0));
    }
}
