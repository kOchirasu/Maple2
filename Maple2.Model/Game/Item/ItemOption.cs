using System.Runtime.InteropServices;
using Maple2.Model.Enum;

namespace Maple2.Model.Game; 

[StructLayout(LayoutKind.Sequential, Pack=2, Size = 10)]
public readonly struct StatOption {
    public readonly StatAttribute Type;
    public readonly int Value;
    public readonly float Percent;

    public StatOption(StatAttribute type, int value) {
        Type = type;
        Value = value;
        Percent = 0;
    }

    public StatOption(StatAttribute type, float percent) {
        Type = type;
        Value = 0;
        Percent = percent;
    }
}

[StructLayout(LayoutKind.Sequential, Pack=2, Size = 10)]
public readonly struct SpecialOption {
    public readonly SpecialAttribute Type;
    public readonly float Value;
    public readonly float Unknown;

    public SpecialOption(SpecialAttribute type, float value) {
        Type = type;
        Value = value;
        Unknown = 0;
    }
}
