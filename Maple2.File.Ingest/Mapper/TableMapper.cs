using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Table;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using JobTable = Maple2.Model.Metadata.JobTable;

namespace Maple2.File.Ingest.Mapper;

public class TableMapper : TypeMapper<TableMetadata> {
    private readonly M2dReader xmlReader;
    private readonly TableParser parser;

    public TableMapper(M2dReader xmlReader) {
        this.xmlReader = xmlReader;
        this.parser = new TableParser(xmlReader);
    }

    protected override IEnumerable<TableMetadata> Map() {
        yield return new TableMetadata {Name = "itembreakingredient.xml", Table = ParseItemBreakIngredient()};
        yield return new TableMetadata {Name = "itemgemstoneupgrade.xml", Table = ParseItemGemstoneUpgrade()};
        yield return new TableMetadata {Name = "job.xml", Table = ParseJobTable()};
    }

    private ItemBreakTable ParseItemBreakIngredient() {
        var results = new Dictionary<int, IReadOnlyList<ItemBreakTable.Ingredient>>();
        foreach ((int itemId, ItemBreakIngredient item) in parser.ParseItemBreakIngredient()) {
            var ingredients = new List<ItemBreakTable.Ingredient>();
            if (item.IngredientItemID1 > 0 && item.IngredientCount1 > 0) {
                ingredients.Add(new ItemBreakTable.Ingredient(item.IngredientItemID1, item.IngredientCount1));
            }
            if (item.IngredientItemID2 > 0 && item.IngredientCount2 > 0) {
                ingredients.Add(new ItemBreakTable.Ingredient(item.IngredientItemID2, item.IngredientCount2));
            }
            if (item.IngredientItemID3 > 0 && item.IngredientCount3 > 0) {
                ingredients.Add(new ItemBreakTable.Ingredient(item.IngredientItemID3, item.IngredientCount3));
            }

            results.Add(itemId, ingredients);
        }

        return new ItemBreakTable(results);
    }

    private GemstoneUpgradeTable ParseItemGemstoneUpgrade() {
        var results = new Dictionary<int, GemstoneUpgradeTable.Entry>();
        foreach ((int itemId, ItemGemstoneUpgrade upgrade) in parser.ParseItemGemstoneUpgrade()) {
            var ingredients = new List<GemstoneUpgradeTable.Ingredient>();
            if (upgrade.IngredientCount1 > 0 && upgrade.IngredientItemID1?.Length > 0 && int.TryParse(upgrade.IngredientItemID1[0], out int itemId1)) {
                ingredients.Add(new GemstoneUpgradeTable.Ingredient(itemId1, upgrade.IngredientCount1));
            }
            if (upgrade.IngredientCount2 > 0 && upgrade.IngredientItemID2?.Length > 0 && int.TryParse(upgrade.IngredientItemID2[0], out int itemId2)) {
                ingredients.Add(new GemstoneUpgradeTable.Ingredient(itemId2, upgrade.IngredientCount2));
            }
            if (upgrade.IngredientCount3 > 0 && upgrade.IngredientItemID3?.Length > 0 && int.TryParse(upgrade.IngredientItemID3[0], out int itemId3)) {
                ingredients.Add(new GemstoneUpgradeTable.Ingredient(itemId3, upgrade.IngredientCount3));
            }
            if (upgrade.IngredientCount4 > 0 && upgrade.IngredientItemID4?.Length > 0 && int.TryParse(upgrade.IngredientItemID4[0], out int itemId4)) {
                ingredients.Add(new GemstoneUpgradeTable.Ingredient(itemId4, upgrade.IngredientCount4));
            }

            results.Add(itemId, new GemstoneUpgradeTable.Entry(upgrade.NextItemID, ingredients));
        }

        return new GemstoneUpgradeTable(results);
    }

    private JobTable ParseJobTable() {
        var results = new Dictionary<JobCode, JobTable.Entry>();
        foreach (File.Parser.Xml.Table.JobTable data in parser.ParseJobTable()) {
            var skills = new Dictionary<SkillRank, JobTable.Skill[]> {
                [SkillRank.Basic] = data.skills.skill
                    .Where(skill => skill.subJobCode <= data.code) // This is not actually correct, but works.
                    .Select(skill => new JobTable.Skill(skill.main, skill.sub, skill.maxLevel, skill.quickSlotPriority))
                    .ToArray(),
                [SkillRank.Awakening] = data.skills.skill
                    .Where(skill => skill.subJobCode > data.code) // This is not actually correct, but works.
                    .Select(skill => new JobTable.Skill(skill.main, skill.sub, skill.maxLevel, skill.quickSlotPriority))
                    .ToArray(),
            };

            results[(JobCode) data.code] = new JobTable.Entry(Tutorial: new JobTable.Tutorial(StartField: data.startField,
                    SkipField: data.tutorialSkipField.Length > 0 ? data.tutorialSkipField[0] : 0,
                    SkipItem: data.tutorialSkipItem,
                    OpenMaps: data.tutorialClearOpenMaps,
                    OpenTaxis: data.tutorialClearOpenTaxis,
                    StartItem: data.startInvenItem.item.Select(item => new JobTable.Item(item.itemID, item.grade, item.count)).ToArray(),
                    Reward: data.reward.item.Select(item => new JobTable.Item(item.itemID, item.grade, 1)).ToArray()),
                Skills: skills,
                BaseSkills: data.learn.SelectMany(learn => learn.skill)
                    .SelectMany(skill => skill.sub.Append(skill.id))
                    .OrderBy(id => id)
                    .ToArray());
        }

        return new JobTable(results);
    }
}
