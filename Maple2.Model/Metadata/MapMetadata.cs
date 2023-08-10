using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record UgcMapGroup(
    int Number,
    int ApartmentNumber,
    int Type,
    UgcMapGroup.Cost ContractCost,
    UgcMapGroup.Cost ExtensionCost,
    UgcMapGroup.Limits Limit) {

    public record Cost(
        int Amount,
        int ItemId,
        int Days);

    public record Limits(
        int Height,
        int Area,
        int Maid,
        int Trigger,
        int InstallNpc,
        int InstallBuilding);
}

#region map
public record MapMetadata(
    int Id,
    string? Name,
    string XBlock,
    MapMetadataProperty Property,
    MapMetadataLimit Limit,
    IReadOnlyList<MapMetadataSpawn> Spawns,
    MapMetadataCashCall CashCall,
    MapEntranceBuff[] EntranceBuffs) : ISearchResult;

public record MapMetadataProperty(
    int Continent,
    MapRegion Region,
    int Category,
    MapType Type,
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
    bool PkMode,
    bool CanFly,
    bool CanClimb);

public record MapMetadataLimit(
    int Capacity,
    short MinLevel,
    short MaxLevel,
    int RequireQuest,
    int[] DisableSkills,
    bool Climb,
    bool Fly,
    bool Move,
    bool FallDamage,
    bool Dash,
    bool Ride,
    bool Pet);

public record MapMetadataSpawn(
    int Id,
    int MinDifficulty,
    int MaxDifficulty,
    int Population,
    int Cooldown,
    string[] Tags,
    int PetPopulation,
    int PetSpawnRate,
    IReadOnlyDictionary<int, int> PetIds); // NpcId => PetId

public record MapMetadataCashCall(
    bool TaxiDeparture,
    bool TaxiDestination,
    bool Medic,
    bool Market,
    bool Recall);

public record MapEntranceBuff(int Id, short Level);
#endregion

#region ugcmap
public record UgcMapMetadata(
    int Id,
    IReadOnlyDictionary<int, UgcMapGroup> Plots);
#endregion
