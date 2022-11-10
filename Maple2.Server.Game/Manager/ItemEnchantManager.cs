using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Serilog;

namespace Maple2.Server.Game.Manager;

public class ItemEnchantManager {
    private const int MAX_RATE = 100;
    private const int CHARGE_RATE = 1;
    // ReSharper disable RedundantExplicitArraySize
    private static readonly int[] RequireFodder = new int[15]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 3, 3, 4};
    private static readonly int[] SuccessRate = new int[15]{100, 100, 100, 95, 90, 80, 70, 60, 50, 40, 30, 20, 15, 10, 5};
    private static readonly int[] FodderRate = new int[15]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 7, 5, 4, 2};
    private static readonly int[] FailCharge = new int[15]{1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 4, 5};
    private static readonly float[] StatBonusRate = new float[15]{0.02f, 0.04f, 0.07f, 0.1f, 0.14f, 0.19f, 0.25f, 0.32f, 0.4f, 0.5f, 0.64f, 0.84f, 1.12f, 1.5f, 2f};

    private static readonly IngredientInfo[][] CatalystCost = new IngredientInfo[15][];
    // ReSharper restore RedundantExplicitArraySize

    // TODO: Dynamic catalysts
    static ItemEnchantManager() {
        CatalystCost[0] = Build(802, 4, 20);
        CatalystCost[1] = Build(802, 4, 20);
        CatalystCost[2] = Build(802, 4, 20);
        CatalystCost[3] = Build(1100, 4, 28);
        CatalystCost[4] = Build(1100, 4, 28);
        CatalystCost[5] = Build(1396, 6, 34);
        CatalystCost[6] = Build(1694, 6, 42);
        CatalystCost[7] = Build(1990, 6, 50);
        CatalystCost[8] = Build(4576, 6, 58);
        CatalystCost[9] = Build(4576, 12, 64);
        CatalystCost[10] = Build(4576, 12, 74);
        CatalystCost[11] = Build(6864, 18, 74);
        CatalystCost[12] = Build(6864, 18, 74);
        CatalystCost[13] = Build(6864, 18, 74);
        CatalystCost[14] = Build(6864, 18, 74);

        IngredientInfo[] Build(int onyx, int chaosOnyx, int crystalFragment) {
            return new[] {
                new IngredientInfo(ItemTag.Onix, onyx),
                new IngredientInfo(ItemTag.ChaosOnix, chaosOnyx),
                new IngredientInfo(ItemTag.CrystalPiece, crystalFragment),
            };
        }
    }

    private readonly GameSession session;
    private readonly Lua.Lua lua;
    private readonly ILogger logger = Log.Logger.ForContext<ItemEnchantManager>();

    public EnchantType Type { get; private set; }

    private Item? upgradeItem;
    private readonly List<IngredientInfo> catalysts;
    private readonly Dictionary<long, Item> fodders;
    private readonly Dictionary<StatAttribute, StatOption> statDeltas;
    private readonly EnchantRates rates;
    private int fodderWeight;
    private int useCharges;

    public ItemEnchantManager(GameSession session, Lua.Lua lua) {
        this.session = session;
        this.lua = lua;

        catalysts = new List<IngredientInfo>();
        fodders = new Dictionary<long, Item>();
        statDeltas = new Dictionary<StatAttribute, StatOption>();
        rates = new EnchantRates();
    }

    public void Reset() {
        Type = EnchantType.None;
        upgradeItem = null;
        catalysts.Clear();
        fodders.Clear();
        statDeltas.Clear();
        rates.Clear();
        fodderWeight = 0;
        useCharges = 0;
    }

    public bool StageItem(EnchantType enchantType, long itemUid) {
        if (enchantType is not (EnchantType.Ophelia or EnchantType.Peachy)) {
            return false;
        }

        Item? item = session.Item.GetGear(itemUid);
        if (item == null || !item.Metadata.Limit.EnableEnchant) {
            session.Send(ItemEnchantPacket.Error(ItemEnchantError.s_itemenchant_invalid_item));
            return false;
        }

        Reset();
        Type = enchantType;
        upgradeItem = item;

        // Ensure this item has an Enchant field set.
        upgradeItem.Enchant ??= new ItemEnchant();
        int enchants = Math.Clamp(upgradeItem.Enchant.Enchants, 0, 15);
        int minFodder = RequireFodder[enchants];

        foreach (IngredientInfo ingredient in GetRequiredCatalysts()) {
            catalysts.Add(ingredient);
        }
        foreach ((StatAttribute attribute, StatOption option) in GetStatOptions()) {
            if (upgradeItem.Enchant.StatOptions.TryGetValue(attribute, out StatOption existing)) {
                statDeltas[attribute] = option - existing;
            } else {
                statDeltas[attribute] = option;
            }
        }

        if (Type is EnchantType.Ophelia) {
            rates.Success = SuccessRate[enchants];
        } else if (Type is EnchantType.Peachy) {
            rates.Success = MAX_RATE;
        }

        session.Send(ItemEnchantPacket.StageItem(Type, upgradeItem, catalysts, statDeltas, rates, minFodder));
        return true;
    }

    public bool UpdateFodder(long itemUid, bool add) {
        if (upgradeItem == null) {
            return false;
        }

        int enchants = Math.Clamp(upgradeItem.Enchant?.Enchants ?? 0, 0, 15);
        if (FodderRate[enchants] <= 0) {
            return false; // If adding fodder does not improve rate, restrict it.
        }

        if (add) {
            // Prevent adding more fodder if it won't help.
            if (rates.Total >= MAX_RATE) {
                return false;
            }
            // Cannot add the same fodder twice.
            if (fodders.ContainsKey(itemUid)) {
                return false;
            }

            Item? item = session.Item.Inventory.Get(itemUid);
            if (item == null) {
                session.Send(ItemEnchantPacket.Error(ItemEnchantError.s_itemenchant_lack_ingredient));
                return false;
            }
            if (!IsValidFodder(upgradeItem, item)) {
                return false;
            }

            fodders.Add(itemUid, item);
            fodderWeight += GetFodderWeight(upgradeItem, item);
        } else {
            if (!fodders.Remove(itemUid, out Item? removed)) {
                return false;
            }

            fodderWeight -= GetFodderWeight(upgradeItem, removed);
        }

        int extra = fodderWeight - RequireFodder[enchants];
        rates.Fodder = Math.Clamp(extra * FodderRate[enchants], 0, MAX_RATE);

        // Recompute charges in case we are over max.
        SetCharges(useCharges);
        session.Send(ItemEnchantPacket.UpdateCharges(fodders.Keys, useCharges, fodderWeight, rates));
        return true;
    }

    public bool UpdateCharges(int count) {
        if (upgradeItem == null || count < 0 || count > upgradeItem.Enchant?.Charges) {
            return false;
        }

        SetCharges(count);
        session.Send(ItemEnchantPacket.UpdateCharges(fodders.Keys, useCharges, fodderWeight, rates));
        return true;
    }

    // TODO: Handle peachy
    public bool Enchant(long itemUid) {
        Item? item = session.Item.GetGear(itemUid);
        if (item == null || upgradeItem == null || upgradeItem.Uid != item.Uid) {
            return false;
        }

        upgradeItem.Enchant ??= new ItemEnchant();
        int enchants = Math.Clamp(upgradeItem.Enchant.Enchants, 0, 15);
        if (fodderWeight < RequireFodder[enchants]) {
            return false;
        }

        lock (session.Item) {
            if (!ConsumeMaterial()) {
                return false;
            }
        }

        float roll = Random.Shared.NextSingle() * MAX_RATE;
        int totalRate = rates.Total;
        bool success = roll < totalRate;
        logger.Debug("Enchant result: {Roll} / {Total} = {Result}", roll, totalRate, success);
        if (success) {
            // GetStatOptions() again to ensure rates match those in table.
            // This *MUST* be called before incrementing Enchants.
            foreach ((StatAttribute attribute, StatOption option) in GetStatOptions()) {
                upgradeItem.Enchant.StatOptions[attribute] = option;
            }
            upgradeItem.Enchant.Enchants++;

            session.Send(ItemEnchantPacket.Success(upgradeItem, statDeltas));
        } else {
            upgradeItem.Enchant.Charges += FailCharge[enchants];
            session.Send(ItemEnchantPacket.Failure(upgradeItem, FailCharge[enchants]));
        }

        Reset();
        return true;
    }

    // session.Item must be locked before calling.
    // Consumes materials and charges for this upgrade.
    private bool ConsumeMaterial() {
        if (upgradeItem?.Enchant == null) {
            return false;
        }

         // Build this index so we don't need to find materials twice.
        Dictionary<ItemTag, IList<Item>> materialsByTag = catalysts.ToDictionary(
            entry => entry.Tag,
            entry => session.Item.Inventory.Filter(item => item.Metadata.Property.Tag == entry.Tag)
        );

        // Validate catalyst + fodder + charges
        if (upgradeItem.Enchant.Charges < useCharges) {
            return false;
        }
        foreach (IngredientInfo catalyst in catalysts) {
            int remaining = catalyst.Amount;
            foreach (Item material in materialsByTag[catalyst.Tag]) {
                remaining -= material.Amount;
                if (remaining <= 0) {
                    break;
                }
            }

            if (remaining > 0) {
                session.Send(ItemEnchantPacket.Error(ItemEnchantError.s_itemenchant_lack_ingredient));
                return false;
            }
        }
        foreach ((long fodderUid, _) in fodders) {
            Item? item = session.Item.Inventory.Get(fodderUid);
            if (item == null) {
                session.Send(ItemEnchantPacket.Error(ItemEnchantError.s_itemenchant_lack_ingredient));
                return false;
            }
        }

        // Consume catalyst + fodder + charges
        foreach (IngredientInfo catalyst in catalysts) {
            int remaining = catalyst.Amount;
            foreach (Item material in materialsByTag[catalyst.Tag]) {
                int consume = Math.Min(remaining, material.Amount);
                if (!session.Item.Inventory.Consume(material.Uid, consume)) {
                    logger.Fatal("Failed to consume item {ItemUid}", material.Uid);
                    throw new InvalidOperationException($"Fatal: Consuming item: {material.Uid}");
                }

                remaining -= consume;
                if (remaining <= 0) {
                    break;
                }
            }
        }
        foreach ((long fodderUid, _) in fodders) {
            if (!session.Item.Inventory.Consume(fodderUid, 1)) {
                logger.Fatal("Failed to consume item {ItemUid}", fodderUid);
                throw new InvalidOperationException($"Fatal: Consuming item: {fodderUid}");
            }
        }
        upgradeItem.Enchant.Charges -= useCharges;

        return true;
    }

    private IEnumerable<IngredientInfo> GetRequiredCatalysts() {
        if (upgradeItem == null) {
            yield break;
        }

        float ratio = 1f;
        if (upgradeItem.Type.IsWeapon) {
            if (upgradeItem.Type.IsDagger || upgradeItem.Type.IsThrowingStar) {
                ratio = 0.5f;
            } else {
                ratio = 1f; // All other weapons
            }
        } else if (upgradeItem.Type.IsArmor) {
            if (upgradeItem.Type.IsOverall) {
                ratio = 1f;
            } if (upgradeItem.Type.IsClothes || upgradeItem.Type.IsPants) {
                ratio = 0.5f;
            } else if (upgradeItem.Type.IsHat) {
                ratio = 0.375f;
            } else if (upgradeItem.Type.IsGloves || upgradeItem.Type.IsShoes) {
                ratio = 0.125f;
            }
        }

        int enchants = upgradeItem.Enchant?.Enchants ?? 0;
        foreach (IngredientInfo ingredient in CatalystCost[enchants]) {
            IngredientInfo modified = ingredient * ratio;
            if (modified.Amount > 0) {
                yield return modified;
            }
        }
    }

    // TODO: Dynamic stat options
    private IEnumerable<(StatAttribute, StatOption)> GetStatOptions() {
        if (upgradeItem == null) {
            yield break;
        }

        int enchants = upgradeItem.Enchant?.Enchants ?? 0;
        if (upgradeItem.Type.IsWeapon) {
            yield return (StatAttribute.MinWeaponAtk, new StatOption(StatBonusRate[enchants]));
            yield return (StatAttribute.MaxWeaponAtk, new StatOption(StatBonusRate[enchants]));
        } else if (upgradeItem.Type.IsArmor) {
            yield return (StatAttribute.Defense, new StatOption(StatBonusRate[enchants]));
        }
    }

    // Prevent user from using more charges than needed
    private void SetCharges(int count) {
        int maxCharges = (int) Math.Ceiling((MAX_RATE - (rates.Total - rates.Charge)) / (float) CHARGE_RATE);
        useCharges = Math.Clamp(count, 0, maxCharges);
        rates.Charge = Math.Clamp(useCharges * CHARGE_RATE, 0, MAX_RATE);
    }

    private static int GetFodderWeight(Item item, Item fodder) {
        if (!item.Type.IsDagger && !item.Type.IsThrowingStar) {
            return 1;
        }

        // For Dagger/ThrowingStar, Toad's Toolkit is valued as 2 fodder.
        return IsValidToolkit(item, fodder) ? 2 : 1;
    }

    private static bool IsValidFodder(Item item, Item fodder) {
        return fodder.Id == item.Id && fodder.Rarity == item.Rarity || IsValidToolkit(item, fodder);
    }

    private static bool IsValidToolkit(Item item, Item fodder) {
        return fodder.Metadata.Property.Tag switch {
            ItemTag.EnchantJockerItemNormal => item.Rarity == 1,
            ItemTag.EnchantJockerItemRare => item.Rarity == 2,
            ItemTag.EnchantJockerItemElite => item.Rarity == 3,
            ItemTag.EnchantJockerItemExcellent => item.Rarity == 4,
            ItemTag.EnchantJockerItemLegendary => item.Rarity == 5,
            ItemTag.EnchantJockerItemEpic => item.Rarity == 6,
            _ => false,
        };
    }
}
