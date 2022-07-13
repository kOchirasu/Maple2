using System;
using System.ComponentModel;

namespace Maple2.Model.Enum;

public enum SkillType : byte {
    Active = 0,
    Passive = 1,
    Special = 2, // Literally only 11000054
    Consumable = 3,
}

public enum SkillSubType : byte {
    Type0 = 0,
    Type1 = 1,
    Type2 = 2,
    Type3 = 3,
    Type4 = 4,
    Type5 = 5,
    Type6 = 6,
    Type7 = 7,
    Type8 = 8,
    Type9 = 9,
}

public enum RangeType : byte {
    None = 0,
    Melee = 1,
    Range = 2,
    Unknown = 3, // 10200282,10200283,35001301
}

public enum Element : byte {
    None = 0,
    Fire = 1,
    Ice = 2,
    Electric = 3,
    Holy = 4,
    Dark = 5,
    Poison = 6,
    Physical = 7,
}

public enum SkillRank : byte {
    Basic = 0,
    Awakening = 1,
    Both = 2,
}

public enum SkillRegion {
    None = 0,
    Box = 1,
    Cylinder = 2,
    Frustum = 3,
    HoleCylinder = 4,
}

public enum SkillEntity {
    None = 0,
    Owner = 1,
    Target = 2,
    Caster = 3,
    PetOwner = 4,
    Attacker = 5,
    RegionBuff = 6,
    RegionDebuff = 7,
    RegionPet = 8,
}
