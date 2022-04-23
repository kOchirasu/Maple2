using System.Runtime.InteropServices;

namespace Maple2.Model.Game; 

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly struct StatOption {
    public readonly int Value;
    public readonly float Percent;

    public StatOption(int value) {
        Value = value;
        Percent = 0;
    }

    public StatOption(float percent) {
        Value = 0;
        Percent = percent;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly struct SpecialOption {
    public readonly float Value;
    public readonly float Unknown;

    public SpecialOption(float value) {
        Value = value;
        Unknown = 0;
    }
}
