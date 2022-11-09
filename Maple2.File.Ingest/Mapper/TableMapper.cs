using System.Numerics;
using Maple2.File.Ingest.Utils;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml;
using Maple2.File.Parser.Xml.Table;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using InteractObject = Maple2.File.Parser.Xml.Table.InteractObject;
using JobTable = Maple2.Model.Metadata.JobTable;
using MagicPath = Maple2.Model.Metadata.MagicPath;

namespace Maple2.File.Ingest.Mapper;

public class TableMapper : TypeMapper<TableMetadata> {
    private readonly TableParser parser;
    private readonly ItemOptionParser optionParser;

    public TableMapper(M2dReader xmlReader) {
        parser = new TableParser(xmlReader);
        optionParser = new ItemOptionParser(xmlReader);
    }

    protected override IEnumerable<TableMetadata> Map() {
        yield return new TableMetadata {Name = "chatemoticon.xml", Table = ParseChatSticker()};
        yield return new TableMetadata {Name = "itembreakingredient.xml", Table = ParseItemBreakIngredient()};
        yield return new TableMetadata {Name = "itemgemstoneupgrade.xml", Table = ParseItemGemstoneUpgrade()};
        yield return new TableMetadata {Name = "job.xml", Table = ParseJobTable()};
        yield return new TableMetadata {Name = "magicpath.xml", Table = ParseMagicPath()};
        yield return new TableMetadata {Name = "instrumentcategoryinfo.xml", Table = ParseInstrument()};
        yield return new TableMetadata {Name = "interactobject.xml", Table = ParseInteractObject(false)};
        yield return new TableMetadata {Name = "interactobject_mastery.xml", Table = ParseInteractObject(true)};
        yield return new TableMetadata {Name = "masteryreceipe.xml", Table = ParseMasteryRecipe()};
        // ItemOption
        yield return new TableMetadata {Name = "itemoptionconstant.xml", Table = ParseItemOptionConstant()};
        yield return new TableMetadata {Name = "itemoptionrandom.xml", Table = ParseItemOptionRandom()};
        yield return new TableMetadata {Name = "itemoptionstatic.xml", Table = ParseItemOptionStatic()};
        yield return new TableMetadata {Name = "itemoptionpick.xml", Table = ParseItemOptionPick()};
        yield return new TableMetadata {Name = "itemoptionvariation.xml", Table = ParseItemVariation()};
        foreach ((string type, ItemEquipVariationTable table) in ParseItemEquipVariation()) {
            yield return new TableMetadata {Name = $"itemoptionvariation_{type}.xml", Table = table};
        }
    }

    private ChatStickerTable ParseChatSticker() {
        var results = new Dictionary<int, ChatStickerMetadata>();
        foreach ((int id, ChatSticker sticker) in parser.ParseChatSticker()) {
            results[id] = new ChatStickerMetadata(
                Id: id,
                GroupId: sticker.group_id);
        }

        return new ChatStickerTable(results);
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
            if (upgrade.IngredientCount1 > 0 && upgrade.IngredientItemID1?.Length > 1) {
                ingredients.Add(new GemstoneUpgradeTable.Ingredient(Enum.Parse<ItemTag>(upgrade.IngredientItemID1[1]), upgrade.IngredientCount1));
            }
            if (upgrade.IngredientCount2 > 0 && upgrade.IngredientItemID2?.Length > 1) {
                ingredients.Add(new GemstoneUpgradeTable.Ingredient(Enum.Parse<ItemTag>(upgrade.IngredientItemID2[1]), upgrade.IngredientCount2));
            }
            if (upgrade.IngredientCount3 > 0 && upgrade.IngredientItemID3?.Length > 1) {
                ingredients.Add(new GemstoneUpgradeTable.Ingredient(Enum.Parse<ItemTag>(upgrade.IngredientItemID3[1]), upgrade.IngredientCount3));
            }
            if (upgrade.IngredientCount4 > 0 && upgrade.IngredientItemID4?.Length > 1) {
                ingredients.Add(new GemstoneUpgradeTable.Ingredient(Enum.Parse<ItemTag>(upgrade.IngredientItemID4[1]), upgrade.IngredientCount4));
            }

            results.Add(itemId, new GemstoneUpgradeTable.Entry(upgrade.GemLevel, upgrade.NextItemID, ingredients));
        }

        return new GemstoneUpgradeTable(results);
    }

    private JobTable ParseJobTable() {
        var results = new Dictionary<JobCode, JobTable.Entry>();
        foreach (Parser.Xml.Table.JobTable data in parser.ParseJobTable()) {
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

    private MagicPathTable ParseMagicPath() {
        var results = new Dictionary<long, IReadOnlyList<MagicPath>>();
        foreach ((long id, MagicType type) in parser.ParseMagicPath()) {
            // Dropping duplicates for now (60073021, 50000303, 5009, 5101)
            if (results.ContainsKey(id)) {
                continue;
            }

            List<MagicPath> moves = type.move.Select(move => new MagicPath(
                Align: move.align,
                AlignHeight: move.alignCubeHeight,
                Rotate: move.rotation,
                IgnoreAdjust: move.ignoreAdjustCubePosition,
                Direction: move.direction != default ? Vector3.Normalize(move.direction) : default,
                FireOffset: move.fireOffsetPosition,
                FireFixed: move.fireFixedPosition,
                TraceTargetOffset: move.traceTargetOffsetPos,
                Velocity: move.vel,
                Distance: move.distance,
                RotateZDegree: move.dirRotZDegree,
                LifeTime: move.lifeTime,
                DelayTime: move.delayTime,
                SpawnTime: move.spawnTime,
                DestroyTime: move.destroyTime
            )).ToList();
            results[id] = moves;
        }

        return new MagicPathTable(results);
    }

    private InstrumentTable ParseInstrument() {
        var categories = new Dictionary<int, (int MidiId, int PercussionId)>();
        foreach ((int _, InstrumentCategoryInfo info) in parser.ParseInstrumentCategoryInfo()) {
            categories[info.id] = (info.GMId, info.percussionId);
        }

        var results = new Dictionary<int, InstrumentMetadata>();
        foreach ((int id, InstrumentInfo info) in parser.ParseInstrumentInfo()) {
            (int midiId, int percussionId) = categories[info.category];
            results[id] = new InstrumentMetadata(
                Id: info.id,
                EquipId: info.equipItemId,
                ScoreCount: info.soloRelayScoreCount,
                Category: info.category,
                MidiId: midiId,
                PercussionId: percussionId);
        }

        return new InstrumentTable(results);
    }

    private InteractObjectTable ParseInteractObject(bool isMastery) {
        var results = new Dictionary<int, InteractObjectMetadata>();
        foreach ((int id, InteractObject info) in isMastery ? parser.ParseInteractObjectMastery() : parser.ParseInteractObject()) {
            var spawn = new InteractObjectMetadataSpawn[info.spawn.code.Length];
            for (int i = 0; i < spawn.Length; i++) {
                spawn[i] = new InteractObjectMetadataSpawn(
                    Id: info.spawn.code[i],
                    Radius: info.spawn.radius[i],
                    Count: info.spawn.count[i],
                    Probability: info.spawn.prop[i],
                    LifeTime: info.spawn.lifeTime[i]);
            }

            results[id] = new InteractObjectMetadata(
                Id: info.id,
                Type: (InteractType) info.type,
                Collection: info.collection,
                ReactCount: info.reactCount,
                TargetPortalId: info.portal.targetPortalId,
                GuildPosterId: info.guild.housePosterId,
                WeaponItemId: info.weapon.weaponItemId,
                Item: new InteractObjectMetadataItem(info.item.code, info.item.consume, info.item.rank, info.item.checkCount, info.gathering.receipeID),
                Time: new InteractObjectMetadataTime(info.time.resetTime, info.time.reactTime, info.time.hideTime),
                AdditionalEffect: new InteractObjectMetadataEffect(
                    Condition: ParseConditional(info.conditionAdditionalEffect),
                    Invoke: ParseInvoke(info.additionalEffect),
                    ModifyCode: info.additionalEffect.modify.code,
                    ModifyTime: info.additionalEffect.modify.modifyTime),
                Spawn: spawn
            );
        }

        return new InteractObjectTable(results);

        InteractObjectMetadataEffect.ConditionEffect[] ParseConditional(InteractObject.ConditionAdditionalEffect additionalEffect) {
            if (additionalEffect.id.Length == 0 || additionalEffect.id[0] == 0) {
                return Array.Empty<InteractObjectMetadataEffect.ConditionEffect>();
            }

            return additionalEffect.id.Zip(additionalEffect.level, (effectId, level) =>
                new InteractObjectMetadataEffect.ConditionEffect(effectId, level)).ToArray();
        }

        InteractObjectMetadataEffect.InvokeEffect[] ParseInvoke(InteractObject.AdditionalEffect additionalEffect) {
            if (additionalEffect.invoke.code.Length == 0 || additionalEffect.invoke.code[0] == 0) {
                return Array.Empty<InteractObjectMetadataEffect.InvokeEffect>();
            }

            return additionalEffect.invoke.code
                .Zip(additionalEffect.invoke.level, (effectId, level) => new {skillId = effectId, level})
                .Zip(additionalEffect.invoke.prop, (effect, prop) =>
                    new InteractObjectMetadataEffect.InvokeEffect(effect.skillId, effect.level, prop))
                .ToArray();
        }
    }

    private ItemOptionConstantTable ParseItemOptionConstant() {
        var results = new Dictionary<int, IReadOnlyDictionary<int, ItemOptionConstant>>();
        foreach (ItemOptionConstantData entry in optionParser.ParseConstant()) {
            var statValues = new Dictionary<StatAttribute, int>();
            var statRates = new Dictionary<StatAttribute, float>();
            foreach (StatAttribute attribute in Enum.GetValues<StatAttribute>()) {
                int value = entry.StatValue((byte) attribute);
                if (value != default) {
                    statValues[attribute] = value;
                }
                float rate = entry.StatRate((byte) attribute);
                if (rate != default) {
                    statRates[attribute] = rate;
                }
            }

            var specialValues = new Dictionary<SpecialAttribute, int>();
            var specialRates = new Dictionary<SpecialAttribute, float>();
            foreach (SpecialAttribute attribute in Enum.GetValues<SpecialAttribute>()) {
                byte index = attribute.OptionIndex();
                if (index == byte.MaxValue) continue;

                SpecialAttribute fixAttribute = attribute.SgiTarget(entry.sgi_target);
                int value = entry.SpecialValue(index);
                if (value != default) {
                    specialValues[fixAttribute] = value;
                }
                float rate = entry.SpecialRate(index);
                if (rate != default) {
                    specialRates[fixAttribute] = rate;
                }
            }

            if (!results.ContainsKey(entry.code)) {
                results[entry.code] = new Dictionary<int, ItemOptionConstant>();
            }

            var option = new ItemOptionConstant(
                Values: statValues,
                Rates: statRates,
                SpecialValues: specialValues,
                SpecialRates: specialRates);
            (results[entry.code] as Dictionary<int, ItemOptionConstant>)!.Add(entry.grade, option);
        }

        return new ItemOptionConstantTable(results);
    }

    private ItemOptionRandomTable ParseItemOptionRandom() {
        return new ItemOptionRandomTable(optionParser.ParseRandom().ToDictionary());
    }

    private ItemOptionStaticTable ParseItemOptionStatic() {
        return new ItemOptionStaticTable(optionParser.ParseStatic().ToDictionary());
    }

    private ItemOptionPickTable ParseItemOptionPick() {
        var results = new Dictionary<int, IReadOnlyDictionary<int, ItemOptionPickTable.Option>>();
        foreach (ItemOptionPick entry in optionParser.ParsePick()) {
            var constantValue = new Dictionary<StatAttribute, int>();
            for (int i = 0; i < entry.constant_value.Length; i += 2) {
                if (string.IsNullOrWhiteSpace(entry.constant_value[i])) continue;
                constantValue.Add(entry.constant_value[i].ToStatAttribute(), int.Parse(entry.constant_value[i + 1]));
            }
            var constantRate = new Dictionary<StatAttribute, int>();
            for (int i = 0; i < entry.constant_rate.Length; i += 2) {
                if (string.IsNullOrWhiteSpace(entry.constant_rate[i])) continue;
                constantRate.Add(entry.constant_rate[i].ToStatAttribute(), int.Parse(entry.constant_rate[i + 1]));
            }
            var staticValue = new Dictionary<StatAttribute, int>();
            for (int i = 0; i < entry.static_value.Length; i += 2) {
                if (string.IsNullOrWhiteSpace(entry.static_value[i])) continue;
                staticValue.Add(entry.static_value[i].ToStatAttribute(), int.Parse(entry.static_value[i + 1]));
            }
            var staticRate = new Dictionary<StatAttribute, int>();
            for (int i = 0; i < entry.static_rate.Length; i += 2) {
                if (string.IsNullOrWhiteSpace(entry.static_rate[i])) continue;
                staticRate.Add(entry.static_rate[i].ToStatAttribute(), int.Parse(entry.static_rate[i + 1]));
            }
            var randomValue = new Dictionary<StatAttribute, int>();
            for (int i = 0; i < entry.random_value.Length; i += 2) {
                if (string.IsNullOrWhiteSpace(entry.random_value[i])) continue;
                randomValue.Add(entry.random_value[i].ToStatAttribute(), int.Parse(entry.random_value[i + 1]));
            }
            var randomRate = new Dictionary<StatAttribute, int>();
            for (int i = 0; i < entry.random_rate.Length; i += 2) {
                if (string.IsNullOrWhiteSpace(entry.random_rate[i])) continue;
                randomRate.Add(entry.random_rate[i].ToStatAttribute(), int.Parse(entry.random_rate[i + 1]));
            }

            if (!results.ContainsKey(entry.optionPickID)) {
                results[entry.optionPickID] = new Dictionary<int, ItemOptionPickTable.Option>();
            }

            var option = new ItemOptionPickTable.Option(constantValue, constantRate, staticValue, staticRate, randomValue, randomRate);
            (results[entry.optionPickID] as Dictionary<int, ItemOptionPickTable.Option>)!.Add(entry.itemGrade, option);
        }
        return new ItemOptionPickTable(results);
    }

    private ItemVariationTable ParseItemVariation() {
        var values = new Dictionary<StatAttribute, ItemVariationTable.Range<int>>();
        var rates = new Dictionary<StatAttribute, ItemVariationTable.Range<float>>();
        var specialValues = new Dictionary<SpecialAttribute, ItemVariationTable.Range<int>>();
        var specialRates = new Dictionary<SpecialAttribute, ItemVariationTable.Range<float>>();
        foreach (ItemOptionVariation.Option option in optionParser.ParseVariation()) {
            string name = option.OptionName;
            if (name.StartsWith("sid")) continue; // Don't know what stat this maps to.

            if (option.OptionValueVariation != 0) {
                var variation = new ItemVariationTable.Range<int>(
                    Min: option.OptionValueMin,
                    Max: option.OptionValueMax,
                    Interval: option.OptionValueVariation);
                try {
                    values[name.ToStatAttribute()] = variation;
                } catch (ArgumentOutOfRangeException) {
                    specialValues[name.ToSpecialAttribute()] = variation;
                }
            } else if (option.OptionRateVariation != 0) {
                if (name.EndsWith("_rate")) {
                    name = name[..^"_rate".Length]; // sanitize suffix
                }

                var variation = new ItemVariationTable.Range<float>(
                    Min: option.OptionRateMin,
                    Max: option.OptionRateMax,
                    Interval: option.OptionRateVariation);
                try {
                    rates[name.ToStatAttribute()] = variation;
                } catch (ArgumentOutOfRangeException) {
                    specialRates[name.ToSpecialAttribute()] = variation;
                }
            }
        }

        return new ItemVariationTable(values, rates, specialValues, specialRates);
    }

    private IEnumerable<(string Type, ItemEquipVariationTable Table)> ParseItemEquipVariation() {
        foreach ((string type, List<ItemOptionVariationEquip.Option> options) in optionParser.ParseVariationEquip()) {
            var values = new Dictionary<StatAttribute, int[]>();
            var rates = new Dictionary<StatAttribute, float[]>();
            var specialValues = new Dictionary<SpecialAttribute, int[]>();
            var specialRates = new Dictionary<SpecialAttribute, float[]>();
            foreach (ItemOptionVariationEquip.Option option in options) {
                string name = option.name.ToLower();
                if (name.EndsWith("value")) {
                    int[] entries = new int[18];
                    for (int i = 0; i < 18; i++) {
                        entries[i] = (int) option[i];
                    }

                    name = name[..^"value".Length]; // Remove suffix
                    try {
                        values.Add(name.ToStatAttribute(), entries);
                    } catch (ArgumentOutOfRangeException) {
                        specialValues.Add(name.ToSpecialAttribute(), entries);
                    }

                } else if (name.EndsWith("rate")) {
                    float[] entries = new float[18];
                    for (int i = 0; i < 18; i++) {
                        entries[i] = option[i];
                    }

                    name = name[..^"rate".Length]; // Remove suffix
                    try {
                        rates.Add(name.ToStatAttribute(), entries);
                    } catch (ArgumentOutOfRangeException) {
                        specialRates.Add(name.ToSpecialAttribute(), entries);
                    }
                } else {
                    throw new ArgumentException($"Invalid option name: {option.name}");
                }
            }

            yield return (type, new ItemEquipVariationTable(values, rates, specialValues, specialRates));
        }
    }

    private MasteryRecipeTable ParseMasteryRecipe() {
        var results = new Dictionary<int, MasteryRecipeTable.Entry>();
        foreach ((long id, MasteryRecipe recipe) in parser.ParseMasteryRecipe()) {
            var requiredItems = new List<MasteryRecipeTable.Ingredient>();
            MasteryRecipeTable.Ingredient? requiredItem1 = ParseMasteryIngredient(recipe.requireItem1);
            if (requiredItem1 != null) requiredItems.Add(requiredItem1);
            MasteryRecipeTable.Ingredient? requiredItem2 = ParseMasteryIngredient(recipe.requireItem2);
            if (requiredItem2 != null) requiredItems.Add(requiredItem2);
            MasteryRecipeTable.Ingredient? requiredItem3 = ParseMasteryIngredient(recipe.requireItem3);
            if (requiredItem3 != null) requiredItems.Add(requiredItem3);
            MasteryRecipeTable.Ingredient? requiredItem4 = ParseMasteryIngredient(recipe.requireItem4);
            if (requiredItem4 != null) requiredItems.Add(requiredItem4);
            MasteryRecipeTable.Ingredient? requiredItem5 = ParseMasteryIngredient(recipe.requireItem5);
            if (requiredItem5 != null) requiredItems.Add(requiredItem5);

            var rewardItems = new List<MasteryRecipeTable.Ingredient>();
            MasteryRecipeTable.Ingredient? rewardItem1 = ParseMasteryIngredient(recipe.rewardItem1);
            if (rewardItem1 != null) rewardItems.Add(rewardItem1);
            MasteryRecipeTable.Ingredient? rewardItem2 = ParseMasteryIngredient(recipe.rewardItem2);
            if (rewardItem2 != null) rewardItems.Add(rewardItem2);
            MasteryRecipeTable.Ingredient? rewardItem3 = ParseMasteryIngredient(recipe.rewardItem3);
            if (rewardItem3 != null) rewardItems.Add(rewardItem3);
            MasteryRecipeTable.Ingredient? rewardItem4 = ParseMasteryIngredient(recipe.rewardItem4);
            if (rewardItem4 != null) rewardItems.Add(rewardItem4);
            MasteryRecipeTable.Ingredient? rewardItem5 = ParseMasteryIngredient(recipe.rewardItem5);
            if (rewardItem5 != null) rewardItems.Add(rewardItem5);

            MasteryRecipeTable.Entry entry = new MasteryRecipeTable.Entry(
                Id: (int) id,
                Type: (MasteryType) recipe.masteryType,
                NoRewardExp: recipe.exceptRewardExp,
                RequiredMastery: recipe.requireMastery,
                RequiredMeso: recipe.requireMeso,
                RequiredQuests: recipe.requireQuest,
                RewardExp: recipe.rewardExp,
                RewardMastery: recipe.rewardMastery,
                HighRateLimitCount: recipe.highPropLimitCount,
                NormalRateLimitCount: recipe.normalPropLimitCount,
                RequiredItems: requiredItems,
                HabitatMapId: recipe.habitatMapId,
                RewardItems: rewardItems);

            results.Add((int) id, entry);
        }

        return new MasteryRecipeTable(results);
    }

    private static MasteryRecipeTable.Ingredient? ParseMasteryIngredient(IReadOnlyList<string> ingredientArray) {
        if (ingredientArray.Count == 0 || ingredientArray[0] == "0") {
            return null;
        }

        string[] idAndTag = ingredientArray[0].Split(":");
        int id = int.Parse(idAndTag[0]);
        string tag = idAndTag.Length > 1 ? idAndTag[1] : string.Empty;
        if (short.TryParse(ingredientArray[1], out short rarity)) {
            rarity = 1;
        }
        if (int.TryParse(ingredientArray[2], out int amount)) {
            amount = 1;
        }

        return new MasteryRecipeTable.Ingredient(
            ItemId: id,
            Rarity: rarity,
            Amount: amount,
            Tag: tag);
    }

    private static MasteryRecipeTable.Ingredient? ParseMasteryIngredient(IReadOnlyList<int> ingredientArray) {
        if (ingredientArray.Count == 0 || ingredientArray[0] == 0) {
            return null;
        }

        return new MasteryRecipeTable.Ingredient(
            ItemId: ingredientArray[0],
            Rarity: (short) ingredientArray[1],
            Amount: ingredientArray[2],
            Tag: string.Empty);
    }
}
