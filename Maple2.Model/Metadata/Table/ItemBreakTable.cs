using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record ItemBreakTable(IReadOnlyDictionary<int, IReadOnlyList<ItemBreakTable.Ingredient>> Entries) : Table(Discriminator.ItemBreakTable) {
    public record Ingredient(int ItemId, int Amount);
}
