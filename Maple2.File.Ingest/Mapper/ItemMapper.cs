using Maple2.Database.Model.Metadata;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Item;
using Maple2.Model.Enum;

namespace Maple2.File.Ingest.Mapper;

public class ItemMapper : TypeMapper<ItemMetadata> {
    private readonly ItemParser parser;

    public ItemMapper(M2dReader xmlReader) {
        parser = new ItemParser(xmlReader);
    }

    protected override IEnumerable<ItemMetadata> Map() {
        foreach ((int id, string name, ItemData data) in parser.Parse()) {
            yield return new ItemMetadata(
                Id:id,
                Name:name,
                SlotNames:data.slots.slot
                    .Where(slot => !string.IsNullOrEmpty(slot.name))
                    .Select(slot => Enum.Parse<EquipSlot>(slot.name, true))
                    .ToArray(),
                Property:new ItemMetadataProperty(
                    IsSkin:data.property.skin != 0,
                    SkinType:data.property.skinType,
                    SlotMax:data.property.slotMax,
                    Type:data.property.type,
                    SubType:data.property.subtype,
                    Group:data.property.itemGroup,
                    Collection:data.property.collection,
                    GearScore:data.property.gearScore,
                    TradableCount:data.property.tradableCount,
                    RepackCount:data.property.rePackingLimitCount,
                    DisableDrop:data.property.disableDrop
                ),
                Limit:new ItemMetadataLimit(
                    Gender:(Gender) data.limit.genderLimit,
                    Level:data.limit.levelLimit,
                    TransferType:data.limit.transferType,
                    ShopSell:data.limit.shopSell != 0,
                    EnableBreak:data.limit.enableBreak != 0,
                    EnableEnchant:data.limit.exceptEnchant != 0,
                    EnableMeretMarket:data.limit.enableRegisterMeratMarket != 0,
                    EnableSocketTransfer:data.limit.enableSocketTransfer,
                    RequireVip:data.limit.vip != 0,
                    RequireWedding:data.limit.wedding != 0,
                    Jobs:data.limit.jobLimit.Select(job => (JobCode)job).ToArray()
                )
            );
        }
    }
}
