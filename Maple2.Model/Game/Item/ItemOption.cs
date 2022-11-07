using System.Runtime.InteropServices;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly record struct StatOption(int Value, float Percent = 0) {
    public StatOption(float percent) : this(0, percent) { }
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly record struct SpecialOption(float Value, float Unknown = 0);
