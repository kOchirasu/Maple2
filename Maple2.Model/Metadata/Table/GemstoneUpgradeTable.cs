using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record GemstoneUpgradeTable(IReadOnlyDictionary<int, GemstoneUpgradeTable.Entry> Entries) : Table(Discriminator.GemstoneUpgradeTable) {
    public record Entry(int NextItemId, IReadOnlyList<Ingredient> Ingredients);
    public record Ingredient(int ItemId, int Amount);
}
