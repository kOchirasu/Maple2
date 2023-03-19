using M2dXmlGenerator;
using Maple2.Database.Extensions;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Item;
using Maple2.File.Parser.Xml.Table;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class ItemMapper : TypeMapper<ItemMetadata> {
    private readonly ItemParser parser;
    private readonly TableParser tableParser;

    public ItemMapper(M2dReader xmlReader) {
        parser = new ItemParser(xmlReader);
        tableParser = new TableParser(xmlReader);
    }

    protected override IEnumerable<ItemMetadata> Map() {
        IEnumerable<(int Id, ItemExtraction Extraction)> itemExtractionData = tableParser.ParseItemExtraction();
        Dictionary<int, int> itemExtractionTryCount = tableParser.ParseItemExtraction()
            .ToDictionary(entry => entry.Id, entry => entry.Extraction.TryCount);

        var itemSetBonuses = new Dictionary<int, List<int>>();
        foreach ((int id, _, SetItemInfo info) in tableParser.ParseSetItemInfo()) {
            foreach (int itemId in info.itemIDs) {
                itemSetBonuses.TryAdd(itemId, new List<int>());
                itemSetBonuses[itemId].Add(id);
            }
        }

        foreach ((int id, string name, ItemData data) in parser.Parse()) {
            int transferType = data.limit.transferType;
            int tradableCount = data.property.tradableCount;
            int tradableCountDeduction = data.property.tradableCountDeduction;
            if (FeatureLocaleFilter.FeatureEnabled("GlobalTransferType")) {
                transferType = data.limit.globalTransferType ?? transferType;
                tradableCount = data.property.globalTradableCount ?? tradableCount;
                tradableCountDeduction = data.property.globalTradableCountDeduction ?? tradableCountDeduction;
            }
            if (FeatureLocaleFilter.FeatureEnabled("GlobalTransferTypeNA")) {
                transferType = data.limit.globalTransferTypeNA ?? transferType;
                tradableCount = data.property.globalTradableCountNA ?? tradableCount;
            }

            long expirationTimestamp = 0;
            if (data.life.expirationPeriod.Length > 0) {
                expirationTimestamp = new DateTime(data.life.expirationPeriod[0], data.life.expirationPeriod[1], data.life.expirationPeriod[2], data.life.expirationPeriod[3], data.life.expirationPeriod[4],
                    data.life.expirationPeriod[5]).ToEpochSeconds();
            }

            long expirationDuration = 0;
            if (data.life.expirationType > 0) {
                expirationDuration = data.life.expirationType switch {
                    1 => // Week
                        DateTime.UnixEpoch.AddDays(7 * data.life.numberOfWeeksMonths).ToEpochSeconds(),
                    2 => // Month
                        DateTime.UnixEpoch.AddMonths(1 * data.life.numberOfWeeksMonths).ToEpochSeconds(),
                    _ => 0
                };
            } else if (data.life.usePeriod > 0) {
                expirationDuration = data.life.usePeriod;
            }

            ItemMetadataSkill? skill = data.skill.skillID == 0 && data.objectWeaponSkill.skillID == 0 ? null : new ItemMetadataSkill(
                Id: data.skill.skillID,
                Level: data.skill.skillID != 0 ? data.skill.skillLevel : (short) 0,
                WeaponId: data.objectWeaponSkill.skillID,
                WeaponLevel: data.objectWeaponSkill.skillID != 0 ? data.objectWeaponSkill.skillLevel : (short) 0);
            ItemMetadataFunction? function = string.IsNullOrWhiteSpace(data.function.name) ? null : new ItemMetadataFunction(
                Type: Enum.Parse<ItemFunction>(data.function.name),
                Name: data.function.name, // Temp duplicate data makes it easier to read DB
                Parameters: data.function.parameter);

            bool hasOption = data.option.@static > 0 || data.option.constant > 0 || data.option.random > 0 || data.option.optionID > 0;
            int levelFactor = (int) data.option.optionLevelFactor;
            if (FeatureLocaleFilter.FeatureEnabled("HiddenStatAdd01") || FeatureLocaleFilter.FeatureEnabled("HiddenStatAdd03") || FeatureLocaleFilter.FeatureEnabled("HiddenStatAdd04")) {
                levelFactor = (int) (data.option.globalOptionLevelFactor ?? levelFactor);
            }
            ItemMetadataOption? option = !hasOption ? null : new ItemMetadataOption(
                StaticId: data.option.@static,
                StaticType: data.option.staticMakeType,
                RandomId: data.option.random,
                RandomType: data.option.randomMakeType,
                ConstantId: data.option.constant,
                ConstantType: data.option.constantMakeType,
                LevelFactor: levelFactor,
                PickId: data.option.optionID);
            ItemMetadataMusic? music = data.property.type != 12 ? null : new ItemMetadataMusic(
                PlayCount: data.MusicScore.playCount,
                MasteryValue: data.MusicScore.masteryValue,
                MasteryValueMax: data.MusicScore.masteryValueMax,
                IsCustomNote: data.MusicScore.isCustomNote,
                NoteLengthMax: data.MusicScore.noteLengthMax,
                FileName: data.MusicScore.fileName,
                PlayTime: data.MusicScore.playTime);
            ItemMetadataHousing? housing = data.property.type != 6 ? null : new ItemMetadataHousing(
                TrophyId: data.housing.trophyID,
                TrophyLevel: data.housing.trophyLevel,
                InteriorLevel: data.housing.interiorLevel);

            yield return new ItemMetadata(
                Id: id,
                Name: name,
                SlotNames: data.slots.slot
                    .Where(slot => !string.IsNullOrEmpty(slot.name))
                    .Select(slot => Enum.Parse<EquipSlot>(slot.name, true))
                    .ToArray(),
                Mesh: data.ucc.mesh,
                Life: new ItemMetadataLife(
                    ExpirationDuration: expirationDuration,
                    ExpirationTimestamp: expirationTimestamp
                ),
                Property: new ItemMetadataProperty(
                    IsSkin: data.property.skin,
                    SkinType: data.property.skinType,
                    SlotMax: data.property.slotMax,
                    Type: data.property.type,
                    SubType: data.property.subtype,
                    Tag: string.IsNullOrWhiteSpace(data.basic.stringTag) ? ItemTag.None : Enum.Parse<ItemTag>(data.basic.stringTag),
                    Group: data.property.itemGroup,
                    Collection: data.property.collection,
                    GearScore: data.property.gearScore,
                    PetId: data.pet.petID,
                    Ride: data.ride.rideMonster,
                    TradableCount: tradableCount,
                    TradableCountDeduction: tradableCountDeduction,
                    RepackCount: data.property.rePackingLimitCount,
                    DisableDrop: data.property.disableDrop,
                    SocketId: data.property.socketDataId,
                    IsFragment: data.property.functionTags == "piece",
                    SetOptionIds: itemSetBonuses.GetValueOrDefault(id)?.ToArray() ?? Array.Empty<int>()
                ),
                Limit: new ItemMetadataLimit(
                    Gender: (Gender) data.limit.genderLimit,
                    Level: data.limit.levelLimit,
                    TransferType: (TransferType) transferType,
                    TradeMaxRarity: data.limit.tradeLimitRank,
                    ShopSell: data.limit.shopSell,
                    EnableBreak: data.limit.enableBreak,
                    EnableEnchant: !data.limit.exceptEnchant,
                    EnableMeretMarket: data.limit.enableRegisterMeratMarket,
                    EnableSocketTransfer: data.limit.enableSocketTransfer,
                    RequireVip: data.limit.vip,
                    RequireWedding: data.limit.wedding,
                    GlamorForgeCount: itemExtractionTryCount.GetValueOrDefault(id),
                    JobLimits: data.limit.jobLimit.Select(job => (JobCode) job).ToArray(),
                    JobRecommends: data.limit.recommendJobs.Select(job => (JobCode) job).ToArray()
                ),
                Skill: skill,
                Function: function,
                Option: option,
                Music: music,
                Housing: housing
            );
        }
    }
}
