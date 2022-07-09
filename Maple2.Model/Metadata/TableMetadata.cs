using System.Text.Json.Serialization;

namespace Maple2.Model.Metadata;

public class TableMetadata {
    public string Name { get; set; }
    public Table Table { get; set; }
}

public abstract partial record Table([JsonDiscriminator] Table.Discriminator Type) {
    public enum Discriminator : uint {
        JobTable = 1,
        ItemBreakTable = 2,
        GemstoneUpgradeTable = 3,
        MagicPathTable = 4,
        InstrumentTable = 5,
    }
}
