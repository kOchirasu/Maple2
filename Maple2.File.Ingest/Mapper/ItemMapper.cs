using M2dXmlGenerator;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Item;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class ItemMapper : TypeMapper<ItemMetadata> {
    private readonly ItemParser parser;

    public ItemMapper(M2dReader xmlReader) {
        parser = new ItemParser(xmlReader);
    }

    protected override IEnumerable<ItemMetadata> Map() {
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

            yield return new ItemMetadata(
                Id: id,
                Name: name,
                SlotNames: data.slots.slot
                    .Where(slot => !string.IsNullOrEmpty(slot.name))
                    .Select(slot => Enum.Parse<EquipSlot>(slot.name, true))
                    .ToArray(),
                Mesh: data.ucc.mesh,
                Property: new ItemMetadataProperty(
                    IsSkin: data.property.skin,
                    SkinType: data.property.skinType,
                    SlotMax: data.property.slotMax,
                    Type: data.property.type,
                    SubType: data.property.subtype,
                    Group: data.property.itemGroup,
                    Collection: data.property.collection,
                    GearScore: data.property.gearScore,
                    TradableCount: tradableCount,
                    TradableCountDeduction: tradableCountDeduction,
                    RepackCount: data.property.rePackingLimitCount,
                    DisableDrop: data.property.disableDrop
                ),
                Limit: new ItemMetadataLimit(
                    Gender: (Gender) data.limit.genderLimit,
                    Level: data.limit.levelLimit,
                    TransferType: transferType,
                    TradeMaxRarity: data.limit.tradeLimitRank,
                    ShopSell: data.limit.shopSell,
                    EnableBreak: data.limit.enableBreak,
                    EnableEnchant: data.limit.exceptEnchant,
                    EnableMeretMarket: data.limit.enableRegisterMeratMarket,
                    EnableSocketTransfer: data.limit.enableSocketTransfer,
                    RequireVip: data.limit.vip,
                    RequireWedding: data.limit.wedding,
                    Jobs: data.limit.jobLimit.Select(job => (JobCode) job).ToArray()
                )
            );
        }
    }
}
