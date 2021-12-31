using System.ComponentModel;

namespace Maple2.Model.Enum;

public enum EquipSlot : sbyte {
    [Description("None")]
    NONE = -1,
    [Description("Hair")]
    HR = 0,
    [Description("Face")]
    FA = 1,
    [Description("Face Decal")]
    FD = 2,
    [Description("Left Hand")]
    LH = 3,
    [Description("Right Hand")]
    RH = 4,
    [Description("Cap")]
    CP = 5,
    [Description("Mantle")]
    MT = 6,
    [Description("Clothes")]
    CL = 7,
    [Description("Pants")]
    PA = 8,
    [Description("Gloves")]
    GL = 9,
    [Description("Shoes")]
    SH = 10,
    [Description("Face Accessory")]
    FH = 11,
    [Description("Eyewear")]
    EY = 12,
    [Description("Earring")]
    EA = 13,
    [Description("Pendant")]
    PD = 14,
    [Description("Ring")]
    RI = 15,
    [Description("Belt")]
    BE = 16,
    [Description("Ear")]
    ER = 17,
    [Description("Off Hand")]
    OH = -2, // Cannot equip in off-hand
}
