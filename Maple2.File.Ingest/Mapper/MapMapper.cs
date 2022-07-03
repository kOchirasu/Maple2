using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Item;
using Maple2.File.Parser.Xml.Map;
using Maple2.Model.Game;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class MapMapper : TypeMapper<MapMetadata> {
    private readonly MapParser parser;

    public MapMapper(M2dReader xmlReader) {
        parser = new MapParser(xmlReader);
    }

    protected override IEnumerable<MapMetadata> Map() {
        foreach ((int id, string name, MapData data) in parser.Parse()) {
            yield return new MapMetadata(
                Id:id,
                Name:name,
                XBlock:data.xblock.name.ToLower(),
                Property:new MapMetadataProperty(
                    Continent:data.property.continentCode,
                    Region:data.property.regionCode,
                    Category:data.property.mapCategoryCode,
                    Type:data.property.mapType,
                    BigCity:data.property.bigCity,
                    ExploreType:data.property.exploreType,
                    TutorialType:data.property.tutorialType,
                    RevivalReturnId:data.property.revivalreturnid,
                    EnterReturnId:data.property.enterreturnid,
                    AutoRevivalType:data.property.autoRevivalType,
                    AutoRevivalTime:data.property.autoRevivalTime,
                    InfiniteMeretRevival:data.property.infinityMeratRevival,
                    NoRevivalHere:data.property.doNotRevivalHere,
                    ReviveFullHp:data.property.recoveryFullHP,
                    UseTimeEvent:data.property.useTimeEvent,
                    HomeReturnable:data.property.homeReturnable,
                    DeathPenalty:data.property.deathPenalty,
                    OnlyDarkTomb:data.property.onlyDarkTomb,
                    PkMode:data.property.pkMode
                ),
                Limit:new MapMetadataLimit(
                    Capacity:data.property.capacity,
                    MinLevel:data.property.enterMinLevel,
                    MaxLevel:data.property.enterMaxLevel,
                    RequireQuest:data.property.requireQuest,
                    DisableSkills:data.property.skillUseDisable,
                    Climb:data.property.checkClimb,
                    Fly:data.property.checkFly,
                    Move:data.property.limitMove,
                    FallDamage:data.ui.fallDamage,
                    Dash:data.ui.useEPSkill,
                    Ride:data.ui.useRidee,
                    Pet:data.ui.usePet
                ),
                CashCall:new MapMetadataCashCall(
                    TaxiDeparture:!data.cashCall.cashTaxiNotDeparture,
                    TaxiDestination:!data.cashCall.cashTaxiNotDestination,
                    Medic:!data.cashCall.cashCallMedicProhibit,
                    Market:!data.cashCall.cashCallMarketProhibit,
                    Recall:!data.cashCall.RecallOtherUserProhibit
                ),
                // TODO: There are also EntranceBuffs for Survival
                EntranceBuffs:data.property.enteranceBuffIDs.Zip(data.property.enteranceBuffLevels,
                    (skillId, level) => new MapEntranceBuff(skillId, (short)level)).ToArray()
            );
        }
    }
}
