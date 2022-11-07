using System;
using System.Text.Json.Serialization;

namespace Maple2.Model.Metadata;

public class TableMetadata {
    public string Name { get; set; }
    public Table Table { get; set; }

    protected bool Equals(TableMetadata other) {
        return Name == other.Name && Table.Equals(other.Table);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;

        return Equals((TableMetadata) obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Name, Table);
    }
}

public abstract partial record Table([JsonDiscriminator] Table.Discriminator Type) {
    public enum Discriminator : uint {
        JobTable = 1,
        ItemBreakTable = 2,
        GemstoneUpgradeTable = 3,
        MagicPathTable = 4,
        InstrumentTable = 5,
        InteractObjectTable = 6,
        ItemOptionConstantTable = 7,
        ItemOptionRandomTable = 8,
        ItemOptionStaticTable = 9,
        ItemOptionPickTable = 10,
        ItemVariationTable = 11,
        ItemEquipVariationTable = 12,
    }
}
