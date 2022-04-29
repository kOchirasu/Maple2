using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Item;
using Maple2.File.Parser.Xml.Map;
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
                XBlock:data.xblock.name,
                Property:new MapMetadataProperty(
                    Continent:data.property.continentCode,
                    Region:data.property.regionCode,
                    Category:data.property.mapCategoryCode,
                    Type:data.property.mapType,
                    BigCity:data.property.bigCity != 0,
                    ExploreType:data.property.exploreType,
                    TutorialType:data.property.tutorialType,
                    RevivalReturnId:data.property.revivalreturnid,
                    EnterReturnId:data.property.enterreturnid,
                    AutoRevivalType:data.property.autoRevivalType,
                    AutoRevivalTime:data.property.autoRevivalTime,
                    InfiniteMeretRevival:data.property.infinityMeratRevival != 0,
                    NoRevivalHere:data.property.doNotRevivalHere != 0,
                    ReviveFullHp:data.property.recoveryFullHP != 0,
                    UseTimeEvent:data.property.useTimeEvent != 0,
                    HomeReturnable:data.property.homeReturnable != 0,
                    DeathPenalty:data.property.deathPenalty != 0,
                    OnlyDarkTomb:data.property.onlyDarkTomb != 0,
                    PkMode:data.property.pkMode != 0
                ),
                Limit:new MapMetadataLimit(
                    Capacity:data.property.capacity,
                    MinLevel:data.property.enterMinLevel,
                    MaxLevel:data.property.enterMaxLevel,
                    RequireQuest:data.property.requireQuest,
                    DisableSkills:data.property.skillUseDisable,
                    Climb:data.property.checkClimb != 0,
                    Fly:data.property.checkFly != 0,
                    Move:data.property.limitMove != 0
                ),
                CashCall:new MapMetadataCashCall(
                    TaxiDeparture:data.cashCall.cashTaxiNotDeparture == 0,
                    TaxiDestination:data.cashCall.cashTaxiNotDestination == 0,
                    Medic:data.cashCall.cashCallMedicProhibit == 0,
                    Market:data.cashCall.cashCallMarketProhibit == 0,
                    Recall: data.cashCall.RecallOtherUserProhibit == 0
                ),
                // TODO: There are also EntranceBuffs for Survival
                EntranceBuffs:data.property.enteranceBuffIDs.Zip(data.property.enteranceBuffLevels, 
                    (skillId, level) => new MapEntranceBuff(skillId, (short)level)).ToArray()
            );
        }
    }
}
