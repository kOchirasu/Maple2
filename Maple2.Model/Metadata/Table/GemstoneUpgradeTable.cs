using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record GemstoneUpgradeTable(IReadOnlyDictionary<int, GemstoneUpgradeTable.Entry> Entries) : Table(Discriminator.GemstoneUpgradeTable) {
    public record Entry(short Level, int NextItemId, IReadOnlyList<Ingredient> Ingredients);
    public record Ingredient(string ItemTag, int Amount);
}
