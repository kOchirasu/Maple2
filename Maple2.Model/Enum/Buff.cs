using System;

namespace Maple2.Model.Enum;

public enum BuffType {
    None = 0,
    Buff = 1,
    Debuff = 2,
}

[Flags]
public enum BuffSubType {
    None = 0,
    Buff = 1, // Used for lots of random things
    Status = 2,
    Damage = 4,
    Motion = 8,
    Recovery = 16,
    Consumable = 32, // Healing, Souvenir, ???
    PcBang = 64,
    Fishing = 128,
    Guild = 256,
    Lapenta = 512,
    Prestige = 1024,
}

public enum BuffCategory {
    None = 0,
    Unknown1 = 1,
    Unknown2 = 2,
    Unknown4 = 4,
    Unknown6 = 6,
    Unknown7 = 7,
    Unknown8 = 8,
    Unknown9 = 9,
    Unknown99 = 99,
    Unknown1007 = 1007,
    Unknown2001 = 2001,
}

public enum BuffEventType {
    None = 0,
    Unknown1 = 1,
    Unknown2 = 2,
    Unknown3 = 3,
    Unknown4 = 4,
}
