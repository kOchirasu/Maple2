using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record SetItemOptionTable(IReadOnlyDictionary<int, SetItemOptionTable.Entry> Entries) : Table
{
    public record Entry(SetItemInfoMetadata Info, SetItemOptionMetadata Option);
}
