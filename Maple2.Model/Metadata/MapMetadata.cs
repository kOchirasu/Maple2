namespace Maple2.Model.Metadata;

public record MapMetadata(
    int Id,
    string? Name,
    string XBlock,
    MapMetadataProperty Property,
    MapMetadataLimit Limit,
    MapMetadataCashCall CashCall,
    MapEntranceBuff[] EntranceBuffs);

public record MapMetadataProperty(
    int Continent,
    int Region,
    int Category,
    int Type,
    bool BigCity,
    int ExploreType,
    int TutorialType,
    int RevivalReturnId,
    int EnterReturnId,
    int AutoRevivalType,
    int AutoRevivalTime,
    bool InfiniteMeretRevival,
    bool NoRevivalHere,
    bool ReviveFullHp,
    bool UseTimeEvent,
    bool HomeReturnable,
    bool DeathPenalty,
    bool OnlyDarkTomb,
    bool PkMode);

public record MapMetadataLimit(
    int Capacity,
    short MinLevel,
    short MaxLevel,
    int RequireQuest,
    int[] DisableSkills,
    bool Climb,
    bool Fly,
    bool Move);

public record MapMetadataCashCall(
    bool TaxiDeparture,
    bool TaxiDestination,
    bool Medic,
    bool Market,
    bool Recall);

public record MapEntranceBuff(int Id, short Level);
