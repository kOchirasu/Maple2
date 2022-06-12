using System.Collections.Generic;

namespace Maple2.Model.Metadata;

#region map
public record MapMetadata(
    int Id,
    string? Name,
    string XBlock,
    MapMetadataProperty Property,
    MapMetadataLimit Limit,
    MapMetadataCashCall CashCall,
    MapEntranceBuff[] EntranceBuffs) : ISearchResult;

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
#endregion

#region ugcmap
public record UgcMapMetadata(
    int Id,
    List<UgcMapGroup> Groups);

public record UgcMapGroup(
    int GroupId,
    int Type,
    int HouseBlock,
    int HouseNumber,
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
#endregion
