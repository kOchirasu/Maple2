using System;
using Maple2.Database.Context;
using Maple2.Model.Metadata;

namespace Maple2.Database.Storage;

public class TableMetadataStorage {
    private readonly Lazy<ItemBreakTable> itemBreakTable;
    private readonly Lazy<GemstoneUpgradeTable> gemstoneUpgradeTable;
    private readonly Lazy<JobTable> jobTable;
    private readonly Lazy<MagicPathTable> magicPathTable;
    private readonly Lazy<InstrumentTable> instrumentTable;
    private readonly Lazy<InteractObjectTable> interactObjectTable;
    private readonly Lazy<InteractObjectTable> interactMasteryTable;

    public ItemBreakTable ItemBreakTable => itemBreakTable.Value;
    public GemstoneUpgradeTable GemstoneUpgradeTable => gemstoneUpgradeTable.Value;
    public JobTable JobTable => jobTable.Value;
    public MagicPathTable MagicPathTable => magicPathTable.Value;
    public InstrumentTable InstrumentTable => instrumentTable.Value;
    public InteractObjectTable InteractObjectTable => interactObjectTable.Value;
    public InteractObjectTable InteractMasteryTable => interactMasteryTable.Value;

    public TableMetadataStorage(MetadataContext context) {
        itemBreakTable = Retrieve<ItemBreakTable>(context, "itembreakingredient.xml");
        gemstoneUpgradeTable = Retrieve<GemstoneUpgradeTable>(context, "itemgemstoneupgrade.xml");
        jobTable = Retrieve<JobTable>(context, "job.xml");
        magicPathTable = Retrieve<MagicPathTable>(context, "magicpath.xml");
        instrumentTable = Retrieve<InstrumentTable>(context, "instrumentcategoryinfo.xml");
        interactObjectTable = Retrieve<InteractObjectTable>(context, "interactobject.xml");
        interactMasteryTable = Retrieve<InteractObjectTable>(context, "interactobject_mastery.xml");
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
