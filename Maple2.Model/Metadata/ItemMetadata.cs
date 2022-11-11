using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record ItemMetadata(
    int Id,
    string? Name,
    EquipSlot[] SlotNames,
    string Mesh,
    int GlamorForgeCount,
    ItemMetadataProperty Property,
    ItemMetadataLimit Limit,
    ItemMetadataSkill? Skill,
    ItemMetadataFunction? Function,
    ItemMetadataOption? Option,
    ItemMetadataMusic? Music,
    ItemMetadataHousing? Housing) : ISearchResult;

public record ItemMetadataProperty(
    bool IsSkin,
    int SkinType, // 99 = Template
    int SlotMax,
    int Type,
    int SubType,
    ItemTag Tag,
    int Group,
    int Collection,
    int GearScore,
    int PetId,
    int Ride,
    int TradableCount,
    int TradableCountDeduction,
    int RepackCount,
    bool DisableDrop);

public record ItemMetadataLimit(
    Gender Gender,
    int Level,
    TransferType TransferType, // [0-7]
    int TradeMaxRarity,
    bool ShopSell,
    bool EnableBreak,
    bool EnableEnchant,
    bool EnableMeretMarket,
    bool EnableSocketTransfer,
    bool RequireVip,
    bool RequireWedding,
    JobCode[] Jobs);

public record ItemMetadataSkill(
    int Id,
    short Level,
    int WeaponId,
    short WeaponLevel);

public record ItemMetadataFunction(
    ItemFunction Type,
    string Name,
    string Parameters);

public record ItemMetadataOption(
    int StaticId,
    int StaticType,
    int RandomId,
    int RandomType,
    int ConstantId,
    int ConstantType,
    int LevelFactor,
    int PickId);

public record ItemMetadataMusic(
    int PlayCount,
    int MasteryValue,
    int MasteryValueMax,
    bool IsCustomNote,
    int NoteLengthMax,
    string FileName,
    int PlayTime);

public record ItemMetadataHousing(
    int TrophyId,
    int TrophyLevel,
    int InteriorLevel);
