namespace Maple2.Model.Enum; 

public enum SkillType : byte {
    Active = 0,
    Passive = 1,
    Unknown = 3, // 9 digit skills
}

public enum SkillSubType : byte {
    Type0 = 0,
    Type1 = 1,
    Type2 = 2,
}

public enum SkillStyle : byte {
    None = 0,
    Melee = 1,
    Range = 2,
}

public enum SkillRank : byte {
    Basic = 0,
    Awakening = 1,
    Both = 2,
}
