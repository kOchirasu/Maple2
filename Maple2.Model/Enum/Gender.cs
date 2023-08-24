using System;

namespace Maple2.Model.Enum;

public enum Gender : byte {
    Male = 0,
    Female = 1,
    All = 2,
}

[Flags]
public enum GenderFlag : byte {
    Male = 1,
    Female = 2,
}
