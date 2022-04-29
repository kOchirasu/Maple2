using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record ItemMetadata(
    int Id,
    string? Name,
    EquipSlot[] SlotNames,
    string Mesh,
    ItemMetadataProperty Property,
    ItemMetadataLimit Limit);

public record ItemMetadataProperty(
    bool IsSkin,
    int SkinType, // 99 = Template
    int SlotMax,
    int Type,
    int SubType,
    int Group,
    int Collection,
    int GearScore,
    int TradableCount,
    int RepackCount,
    bool DisableDrop);

public record ItemMetadataLimit(
    Gender Gender,
    int Level,
    int TransferType, // [0-7]
    bool ShopSell,
    bool EnableBreak,
    bool EnableEnchant,
    bool EnableMeretMarket,
    bool EnableSocketTransfer,
    bool RequireVip,
    bool RequireWedding,
    JobCode[] Jobs);
