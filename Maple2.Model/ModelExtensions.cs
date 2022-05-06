using System;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;

namespace Maple2.Model;

public static class ModelExtensions {
    public static JobCode Code(this Job job) {
        return (JobCode) ((int) job / 10);
    }

    public static InventoryType Inventory(this ItemMetadata metadata) {
        return metadata.Property.Type switch {
            0 => metadata.Property.SubType == 2 ? InventoryType.Consumable : InventoryType.Misc, // Unknown
            1 => metadata.Property.IsSkin ? InventoryType.Outfit : InventoryType.Gear,
            2 => metadata.Property.SubType == 2 ? InventoryType.Consumable : InventoryType.Misc,
            3 => InventoryType.Quest,
            4 => metadata.Property.SubType == 2 ? InventoryType.Consumable : InventoryType.Misc,
            5 => InventoryType.Mount, // Air mount
            6 => InventoryType.FishingMusic, // Furnishing shows up in FishingMusic
            7 => InventoryType.Badge,
            9 => InventoryType.Mount, // Ground mount
            10 => metadata.Property.SubType != 20 ? InventoryType.Misc : InventoryType.FishingMusic,
            11 => InventoryType.Pets,
            12 => InventoryType.FishingMusic, // Music Score
            13 => InventoryType.Gemstone,
            14 => InventoryType.Gemstone, // Gem dust
            15 => InventoryType.Catalyst,
            16 => InventoryType.LifeSkill,
            17 => (InventoryType) 8, // Tab 8
            18 => InventoryType.Consumable,
            19 => InventoryType.Catalyst,
            20 => InventoryType.Currency,
            21 => InventoryType.Lapenshard,
            22 => InventoryType.Misc, // Blueprint
            _ => throw new ArgumentException(
                $"Unknown Tab for: {metadata.Property.Type},{metadata.Property.SubType}")
        };
    }

    public static bool IsMeso(this Item item) => item.Id is >= 90000001 and <= 90000003;
    public static bool IsMeret(this Item item) => item.Id is 90000004 or 90000011 or 90000015 or 90000016;
    public static bool IsExp(this Item item) => item.Id is 90000008;
    public static bool IsSpirit(this Item item) => item.Id is 90000009;
    public static bool IsStamina(this Item item) => item.Id is 90000010;
}
