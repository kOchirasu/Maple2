using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model;
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

    public ItemStats? Compute(ItemMetadata item, int rarity) {
        if (item.Option == null) {
            return null;
        }

        ItemOptionPickTable.Option? pick = TableMetadata.ItemOptionPickTable.Options.GetValueOrDefault(item.Option.PickId, rarity);
        var itemType = new ItemType(item.Id);
        int job = (int) item.Limit.Jobs.FirstOrDefault(JobCode.Newbie);
        int levelFactor = item.Option.LevelFactor;
        ushort level = (ushort) item.Limit.Level;

        var statOption = new Dictionary<StatAttribute, StatOption>?[ItemStats.TYPE_COUNT];
        var specialOption = new Dictionary<SpecialAttribute, SpecialOption>?[ItemStats.TYPE_COUNT];
        for (int i = 0; i < ItemStats.TYPE_COUNT; i++) {
            var type = (ItemStats.Type) i;
            switch (type) {
                case ItemStats.Type.Constant: {
                    if (pick == null) {
                        continue;
                    }
                    var result = new Dictionary<StatAttribute, StatOption>();
                    foreach ((StatAttribute attribute, int deviation) in pick.ConstantValue) {
                        result[attribute] = new StatOption(ConstValue(attribute, deviation, itemType.Type, job, levelFactor, rarity, level));
                    }
                    statOption[i] = result;
                    break;
                }
                case ItemStats.Type.Static: {
                    if (pick == null) {
                        continue;
                    }
                    var result = new Dictionary<StatAttribute, StatOption>();
                    foreach ((StatAttribute attribute, int deviation) in pick.StaticValue) {
                        result[attribute] = new StatOption(StaticValue(attribute, deviation, itemType.Type, job, levelFactor, rarity, level));
                    }
                    foreach ((StatAttribute attribute, int deviation) in pick.StaticRate) {
                        result[attribute] = new StatOption(StaticRate(attribute, deviation, itemType.Type, job, levelFactor, rarity, level));
                    }
                    statOption[i] = result;
                    break;
                }
                case ItemStats.Type.Random: {
                    if (!TableMetadata.ItemOptionRandomTable.Options.TryGetValue(item.Option.RandomId, rarity, out ItemOption? option)) {
                        break;
                    }

                    (statOption[i], specialOption[i]) = RandomItemOption(option);
                    break;
                }
            }
        }

        return new ItemStats(statOption, specialOption);
    }

    private ItemEquipVariationTable? GetVariationTable(ItemType type) {
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

    private static (Dictionary<StatAttribute, StatOption>, Dictionary<SpecialAttribute, SpecialOption>) RandomItemOption(ItemOption option) {
        var statResult = new Dictionary<StatAttribute, StatOption>();
        var specialResult = new Dictionary<SpecialAttribute, SpecialOption>();

        int total = Random.Shared.Next(option.NumPick.Min, option.NumPick.Max + 1);
        if (total == 0) {
            return (statResult, specialResult);
        }

        // Ensures that there are enough options to choose.
        total = Math.Min(total, option.Entries.Length);
        while (statResult.Count + specialResult.Count < total) {
            int index = Random.Shared.Next(0, option.Entries.Length);
            ItemOption.Entry entry = option.Entries[index];
            if (entry.StatAttribute == null && entry.SpecialAttribute == null || entry.Values == null && entry.Rates == null) {
                Log.Error("Failed to select random item option: {Entry}", entry); // Invalid entry
                return (statResult, specialResult);
            }

            if (entry.StatAttribute is not null) {
                var attribute = (StatAttribute) entry.StatAttribute;
                if (statResult.ContainsKey(attribute)) continue; // No duplicates allowed.

                if (entry.Values != null) {
                    statResult.Add(attribute, new StatOption(Random.Shared.Next(entry.Values.Value.Min, entry.Values.Value.Max + 1)));
                } else if (entry.Rates != null) {
                    float delta = entry.Rates.Value.Max - entry.Rates.Value.Min;
                    statResult.Add(attribute, new StatOption(Random.Shared.NextSingle() * delta + entry.Rates.Value.Min));
                }
            } else if (entry.SpecialAttribute is not null) {
                var attribute = (SpecialAttribute) entry.SpecialAttribute;
                if (specialResult.ContainsKey(attribute)) continue; // No duplicates allowed.

                if (entry.Values != null) {
                    specialResult.Add(attribute, new SpecialOption(Random.Shared.Next(entry.Values.Value.Min, entry.Values.Value.Max + 1)));
                } else if (entry.Rates != null) {
                    float delta = entry.Rates.Value.Max - entry.Rates.Value.Min;
                    specialResult.Add(attribute, new SpecialOption(Random.Shared.NextSingle() * delta + entry.Rates.Value.Min));
                }
            }
        }

        return (statResult, specialResult);
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
