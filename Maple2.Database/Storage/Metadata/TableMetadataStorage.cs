using System;
using Maple2.Database.Context;
using Maple2.Model.Metadata;

namespace Maple2.Database.Storage;

public class TableMetadataStorage {
    private readonly Lazy<ChatStickerTable> chatStickerTable;
    private readonly Lazy<ItemBreakTable> itemBreakTable;
    private readonly Lazy<GemstoneUpgradeTable> gemstoneUpgradeTable;
    private readonly Lazy<JobTable> jobTable;
    private readonly Lazy<MagicPathTable> magicPathTable;
    private readonly Lazy<InstrumentTable> instrumentTable;
    private readonly Lazy<InteractObjectTable> interactObjectTable;
    private readonly Lazy<InteractObjectTable> interactMasteryTable;
    private readonly Lazy<ItemOptionConstantTable> itemOptionConstantTable;
    private readonly Lazy<ItemOptionRandomTable> itemOptionRandomTable;
    private readonly Lazy<ItemOptionStaticTable> itemOptionStaticTable;
    private readonly Lazy<ItemOptionPickTable> itemOptionPickTable;
    private readonly Lazy<ItemVariationTable> itemVariationTable;
    private readonly Lazy<ItemEquipVariationTable> accVariationTable;
    private readonly Lazy<ItemEquipVariationTable> armorVariationTable;
    private readonly Lazy<ItemEquipVariationTable> petVariationTable;
    private readonly Lazy<ItemEquipVariationTable> weaponVariationTable;

    public ChatStickerTable ChatStickerTable => chatStickerTable.Value;
    public ItemBreakTable ItemBreakTable => itemBreakTable.Value;
    public GemstoneUpgradeTable GemstoneUpgradeTable => gemstoneUpgradeTable.Value;
    public JobTable JobTable => jobTable.Value;
    public MagicPathTable MagicPathTable => magicPathTable.Value;
    public InstrumentTable InstrumentTable => instrumentTable.Value;
    public InteractObjectTable InteractObjectTable => interactObjectTable.Value;
    public InteractObjectTable InteractMasteryTable => interactMasteryTable.Value;
    public ItemOptionConstantTable ItemOptionConstantTable => itemOptionConstantTable.Value;
    public ItemOptionRandomTable ItemOptionRandomTable => itemOptionRandomTable.Value;
    public ItemOptionStaticTable ItemOptionStaticTable => itemOptionStaticTable.Value;
    public ItemOptionPickTable ItemOptionPickTable => itemOptionPickTable.Value;
    public ItemVariationTable ItemVariationTable => itemVariationTable.Value;
    public ItemEquipVariationTable AccessoryVariationTable => accVariationTable.Value;
    public ItemEquipVariationTable ArmorVariationTable => armorVariationTable.Value;
    public ItemEquipVariationTable PetVariationTable => petVariationTable.Value;
    public ItemEquipVariationTable WeaponVariationTable => weaponVariationTable.Value;

    public TableMetadataStorage(MetadataContext context) {
        chatStickerTable = Retrieve<ChatStickerTable>(context, "chatemoticon.xml");
        itemBreakTable = Retrieve<ItemBreakTable>(context, "itembreakingredient.xml");
        gemstoneUpgradeTable = Retrieve<GemstoneUpgradeTable>(context, "itemgemstoneupgrade.xml");
        jobTable = Retrieve<JobTable>(context, "job.xml");
        magicPathTable = Retrieve<MagicPathTable>(context, "magicpath.xml");
        instrumentTable = Retrieve<InstrumentTable>(context, "instrumentcategoryinfo.xml");
        interactObjectTable = Retrieve<InteractObjectTable>(context, "interactobject.xml");
        interactMasteryTable = Retrieve<InteractObjectTable>(context, "interactobject_mastery.xml");

        itemOptionConstantTable = Retrieve<ItemOptionConstantTable>(context, "itemoptionconstant.xml");
        itemOptionRandomTable = Retrieve<ItemOptionRandomTable>(context, "itemoptionrandom.xml");
        itemOptionStaticTable = Retrieve<ItemOptionStaticTable>(context, "itemoptionstatic.xml");
        itemOptionPickTable = Retrieve<ItemOptionPickTable>(context, "itemoptionpick.xml");
        itemVariationTable = Retrieve<ItemVariationTable>(context, "itemoptionvariation.xml");
        accVariationTable = Retrieve<ItemEquipVariationTable>(context, "itemoptionvariation_acc.xml");
        armorVariationTable = Retrieve<ItemEquipVariationTable>(context, "itemoptionvariation_armor.xml");
        petVariationTable = Retrieve<ItemEquipVariationTable>(context, "itemoptionvariation_pet.xml");
        weaponVariationTable = Retrieve<ItemEquipVariationTable>(context, "itemoptionvariation_weapon.xml");
    }

    private static Lazy<T> Retrieve<T>(MetadataContext context, string key) where T : Table {
        var result = new Lazy<T>(() => {
            lock (context) {
                TableMetadata? row = context.TableMetadata.Find(key);
                if (row?.Table is not T result) {
                    throw new InvalidOperationException($"Row does not exist: {key}");
                }

                return result;
            }
        });

#if !DEBUG
        // No lazy loading for RELEASE build.
        _ = result.Value;
#endif
        return result;
    }
}
