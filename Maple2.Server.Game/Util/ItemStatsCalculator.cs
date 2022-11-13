using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Tools.Extensions;
using Serilog;

namespace Maple2.Server.Game.Util;

public sealed class ItemStatsCalculator {
    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    public Lua.Lua Lua { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public ItemStats? Compute(Item item) {
        if (item.Metadata.Option == null) {
            return null;
        }

        var stats = new ItemStats();
        int job = (int) item.Metadata.Limit.Jobs.FirstOrDefault(JobCode.Newbie);
        int levelFactor = item.Metadata.Option.LevelFactor;
        ushort level = (ushort) item.Metadata.Limit.Level;

        ItemOptionPickTable.Option? pick = TableMetadata.ItemOptionPickTable.Options.GetValueOrDefault(item.Metadata.Option.PickId, item.Rarity);
        if (pick != null) {
            var constantResult = new Dictionary<StatAttribute, StatOption>();
            foreach ((StatAttribute attribute, int deviation) in pick.ConstantValue) {
                int value = ConstValue(attribute, deviation, item.Type.Type, job, levelFactor, item.Rarity, level);
                if (value > 0) {
                    constantResult[attribute] = new StatOption(value);
                }
            }
            stats[ItemStats.Type.Constant] = new ItemStats.Option(statOption: constantResult);

            var staticResult = new Dictionary<StatAttribute, StatOption>();
            foreach ((StatAttribute attribute, int deviation) in pick.StaticValue) {
                int value = StaticValue(attribute, deviation, item.Type.Type, job, levelFactor, item.Rarity, level);
                if (value > 0) {
                    staticResult[attribute] = new StatOption(value);
                }
            }
            foreach ((StatAttribute attribute, int deviation) in pick.StaticRate) {
                float rate = StaticRate(attribute, deviation, item.Type.Type, job, levelFactor, item.Rarity, level);
                if (rate > 0) {
                    staticResult[attribute] = new StatOption(rate);
                }
            }
            stats[ItemStats.Type.Static] = new ItemStats.Option(statOption: staticResult);
        }

        if (GetRandomOption(item, out ItemStats.Option? option)) {
            RandomizeValues(item.Type, ref option);
            stats[ItemStats.Type.Random] = option;
        }

        return stats;
    }

    public bool UpdateRandomOption(ref Item item, params LockOption[] presets) {
        if (item.Metadata.Option == null || item.Stats == null) {
            return false;
        }

        ItemStats.Option option = item.Stats[ItemStats.Type.Random];
        if (option.Count == 0) {
            return false;
        }

        // Get some random options
        if (!GetRandomOption(item, out ItemStats.Option? randomOption, option.Count, presets)) {
            return false;
        }

        if (!RandomizeValues(item.Type, ref randomOption)) {
            return false;
        }

        // Restore locked values.
        foreach (LockOption lockOption in presets) {
            if (lockOption.TryGet(out StatAttribute basic, out bool lockBasicValue)) {
                if (lockBasicValue) {
                    Debug.Assert(randomOption.Basic.ContainsKey(basic), "Missing basic attribute after using lock.");
                    randomOption.Basic[basic] = option.Basic[basic];
                }
            } else if (lockOption.TryGet(out SpecialAttribute special, out bool lockSpecialValue)) {
                if (lockSpecialValue) {
                    Debug.Assert(randomOption.Special.ContainsKey(special), "Missing special attribute after using lock.");
                    randomOption.Special[special] = option.Special[special];
                }
            }
        }

        // Update item with result.
        item.Stats[ItemStats.Type.Random] = randomOption;
        return true;
    }

    // TODO: These should technically be weighted towards the lower end.
    private bool RandomizeValues(in ItemType type, ref ItemStats.Option option) {
        ItemEquipVariationTable? table = GetVariationTable(type);
        if (table == null) {
            return false;
        }

        foreach (StatAttribute attribute in option.Basic.Keys) {
            int index = Random.Shared.Next(2, 18);
            if (table.Values.TryGetValue(attribute, out int[]? values)) {
                int value = values.ElementAtOrDefault(index);
                option.Basic[attribute] = new StatOption(value);
            } else if (table.Rates.TryGetValue(attribute, out float[]? rates)) {
                float rate = rates.ElementAtOrDefault(index);
                option.Basic[attribute] = new StatOption(rate);
            }
        }
        foreach (SpecialAttribute attribute in option.Special.Keys) {
            int index = Random.Shared.Next(2, 18);
            if (table.SpecialValues.TryGetValue(attribute, out int[]? values)) {
                int value = values.ElementAtOrDefault(index);
                option.Special[attribute] = new SpecialOption(0f, value);
            } else if (table.SpecialRates.TryGetValue(attribute, out float[]? rates)) {
                float rate = rates.ElementAtOrDefault(index);
                option.Special[attribute] = new SpecialOption(rate);
            }
        }

        return true;
    }

    // Used to calculate the default random attributes for a given item.
    private bool GetRandomOption(Item item, [NotNullWhen(true)] out ItemStats.Option? option, int count = -1, params LockOption[] presets) {
        if (item.Metadata.Option == null) {
            option = null;
            return false;
        }

        if (TableMetadata.ItemOptionRandomTable.Options.TryGetValue(item.Metadata.Option.RandomId, item.Rarity, out ItemOption? itemOption)) {
            option = RandomItemOption(itemOption, count, presets);
            return true;
        }

        option = null;
        return false;
    }

    private ItemEquipVariationTable? GetVariationTable(in ItemType type) {
        if (type.IsAccessory) {
            return TableMetadata.AccessoryVariationTable;
        }
        if (type.IsArmor) {
            return TableMetadata.ArmorVariationTable;
        }
        if (type.IsWeapon) {
            return TableMetadata.WeaponVariationTable;
        }
        if (type.IsCombatPet) { // StoragePet cannot have variations
            return TableMetadata.PetVariationTable;
        }

        return null;
    }

    private static ItemStats.Option RandomItemOption(ItemOption option, int count = -1, params LockOption[] presets) {
        var statResult = new Dictionary<StatAttribute, StatOption>();
        var specialResult = new Dictionary<SpecialAttribute, SpecialOption>();

        int total = count < 0 ? Random.Shared.Next(option.NumPick.Min, option.NumPick.Max + 1) : count;
        if (total == 0) {
            return new ItemStats.Option(statResult, specialResult);
        }
        // Ensures that there are enough options to choose.
        total = Math.Min(total, option.Entries.Length);

        // Compute locked options first.
        foreach (LockOption preset in presets) {
            if (preset.TryGet(out StatAttribute basic, out bool _)) {
                ItemOption.Entry entry = option.Entries.FirstOrDefault(e => e.StatAttribute == basic);
                // Ignore any invalid presets, they will get populated with valid data below.
                AddResult(entry, statResult, specialResult);
            } else if (preset.TryGet(out SpecialAttribute special, out bool _)) {
                ItemOption.Entry entry = option.Entries.FirstOrDefault(e => e.SpecialAttribute == special);
                // Ignore any invalid presets, they will get populated with valid data below.
                AddResult(entry, statResult, specialResult);
            }
        }

        while (statResult.Count + specialResult.Count < total) {
            int index = Random.Shared.Next(0, option.Entries.Length);
            ItemOption.Entry entry = option.Entries[index];
            if (!AddResult(entry, statResult, specialResult)) {
                Log.Error("Failed to select random item option: {Entry}", entry); // Invalid entry
            }
        }

        return new ItemStats.Option(statResult, specialResult);

        // Helper function
        bool AddResult(ItemOption.Entry entry, IDictionary<StatAttribute, StatOption> statDict, IDictionary<SpecialAttribute, SpecialOption> specialDict) {
            if (entry.StatAttribute == null && entry.SpecialAttribute == null || entry.Values == null && entry.Rates == null) {
                return false;
            }

            if (entry.StatAttribute != null) {
                var attribute = (StatAttribute) entry.StatAttribute;
                if (statDict.ContainsKey(attribute)) return true; // Cannot add duplicate values, retry.

                if (entry.Values != null) {
                    statDict.Add(attribute, new StatOption(Random.Shared.Next(entry.Values.Value.Min, entry.Values.Value.Max + 1)));
                } else if (entry.Rates != null) {
                    float delta = entry.Rates.Value.Max - entry.Rates.Value.Min;
                    statDict.Add(attribute, new StatOption(Random.Shared.NextSingle() * delta + entry.Rates.Value.Min));
                }
                return true;
            }
            if (entry.SpecialAttribute != null) {
                var attribute = (SpecialAttribute) entry.SpecialAttribute;
                if (specialDict.ContainsKey(attribute)) return true; // Cannot add duplicate values, retry.

                if (entry.Values != null) {
                    specialDict.Add(attribute, new SpecialOption(0f, Random.Shared.Next(entry.Values.Value.Min, entry.Values.Value.Max + 1)));
                } else if (entry.Rates != null) {
                    float delta = entry.Rates.Value.Max - entry.Rates.Value.Min;
                    specialDict.Add(attribute, new SpecialOption(Random.Shared.NextSingle() * delta + entry.Rates.Value.Min));
                }
                return true;
            }
            return false;
        }
    }

    private int ConstValue(StatAttribute attribute, int deviation, int type, int job, int levelFactor, int rarity, ushort level) {
        (float, float) range = attribute switch {
            StatAttribute.Strength => Lua.ConstantValueStr(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.Dexterity => Lua.ConstantValueDex(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.Intelligence => Lua.ConstantValueInt(0, deviation, type, job, levelFactor, rarity, level, 1), // TODO: handle a7
            StatAttribute.Luck => Lua.ConstantValueLuk(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.Health => Lua.ConstantValueHp(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.CriticalRate => Lua.ConstantValueCap(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.Defense => Lua.ConstantValueNdd(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.MagicalAtk => Lua.ConstantValueMap(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.PhysicalRes => Lua.ConstantValuePar(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.MagicalRes => Lua.ConstantValueMar(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.MinWeaponAtk => Lua.ConstantValueWapMin(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.MaxWeaponAtk => Lua.ConstantValueWapMax(0, deviation, type, job, levelFactor, rarity, level),
            _ => (0, 0),
        };
        return (int) Math.Max(range.Item1, range.Item2);
    }

    private int StaticValue(StatAttribute attribute, int deviation, int type, int job, int levelFactor, int rarity, ushort level) {
        (float, float) range = attribute switch {
            StatAttribute.Health => Lua.StaticValueHp(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.Defense => Lua.StaticValueNdd(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.PhysicalAtk => Lua.StaticValuePap(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.MagicalAtk => Lua.StaticValueMap(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.PhysicalRes => Lua.StaticValuePar(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.MagicalRes => Lua.StaticValueMar(0, deviation, type, job, levelFactor, rarity, level),
            StatAttribute.MaxWeaponAtk => Lua.StaticValueWapMax(0, deviation, type, job, levelFactor, rarity, level),
            _ => (0, 0),
        };
        return Random.Shared.Next((int) range.Item1, (int) range.Item2 + 1);
    }

    private float StaticRate(StatAttribute attribute, int deviation, int type, int job, int levelFactor, int rarity, ushort level) {
        (float, float) range = attribute switch {
            StatAttribute.PerfectGuard => Lua.StaticRateAbp(0, deviation, type, job, levelFactor, rarity, level),
            _ => (0, 0),
        };
        return Random.Shared.NextSingle() * (range.Item2 - range.Item1) + range.Item1;
    }
}
