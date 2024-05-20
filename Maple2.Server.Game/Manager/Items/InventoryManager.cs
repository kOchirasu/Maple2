﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Serilog;
using static Maple2.Model.Error.ItemInventoryError;

namespace Maple2.Server.Game.Manager.Items;

public class InventoryManager {
    private const int BATCH_SIZE = 10;

    private readonly GameSession session;

    private readonly Dictionary<InventoryType, ItemCollection> tabs;
    private readonly List<Item> delete;

    public InventoryManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        tabs = new Dictionary<InventoryType, ItemCollection>();
        foreach (InventoryType type in Enum.GetValues<InventoryType>()) {
            session.Player.Value.Unlock.Expand.TryGetValue(type, out short expand);
            tabs[type] = new ItemCollection((short) (BaseSize(type) + expand));
        }

        delete = [];
        foreach ((InventoryType type, List<Item> load) in db.GetInventory(session.CharacterId)) {
            if (tabs.TryGetValue(type, out ItemCollection? items)) {
                foreach (Item item in load) {
                    if (items.Add(item).Count == 0) {
                        Log.Error("Failed to add item:{Uid}", item.Uid);
                    }
                }
            }
        }
    }

    private static short BaseSize(InventoryType type) {
        return type switch {
            InventoryType.Gear => Constant.BagSlotTabGameCount,
            InventoryType.Outfit => Constant.BagSlotTabSkinCount,
            InventoryType.Mount => Constant.BagSlotTabSummonCount,
            InventoryType.Catalyst => Constant.BagSlotTabMaterialCount,
            InventoryType.FishingMusic => Constant.BagSlotTabLifeCount,
            InventoryType.Quest => Constant.BagSlotTabQuestCount,
            InventoryType.Gemstone => Constant.BagSlotTabGemCount,
            InventoryType.Misc => Constant.BagSlotTabMiscCount,
            InventoryType.LifeSkill => Constant.BagSlotTabMasteryCount,
            InventoryType.Pets => Constant.BagSlotTabPetCount,
            InventoryType.Consumable => Constant.BagSlotTabActiveSkillCount,
            InventoryType.Currency => Constant.BagSlotTabCoinCount,
            InventoryType.Badge => Constant.BagSlotTabBadgeCount,
            InventoryType.Lapenshard => Constant.BagSlotTabLapenshardCount,
            InventoryType.Fragment => Constant.BagSlotTabPieceCount,
            _ => throw new ArgumentOutOfRangeException($"Invalid InventoryType: {type}"),
        };
    }

    private static short MaxExpandSize(InventoryType type) {
        return type switch {
            InventoryType.Gear => Constant.BagSlotTabGameCountMax,
            InventoryType.Outfit => Constant.BagSlotTabSkinCountMax,
            InventoryType.Mount => Constant.BagSlotTabSummonCountMax,
            InventoryType.Catalyst => Constant.BagSlotTabMaterialCountMax,
            InventoryType.FishingMusic => Constant.BagSlotTabLifeCountMax,
            InventoryType.Quest => Constant.BagSlotTabQuestCountMax,
            InventoryType.Gemstone => Constant.BagSlotTabGemCountMax,
            InventoryType.Misc => Constant.BagSlotTabMiscCountMax,
            InventoryType.LifeSkill => Constant.BagSlotTabMasteryCountMax,
            InventoryType.Pets => Constant.BagSlotTabPetCountMax,
            InventoryType.Consumable => Constant.BagSlotTabActiveSkillCountMax,
            InventoryType.Currency => Constant.BagSlotTabCoinCountMax,
            InventoryType.Badge => Constant.BagSlotTabBadgeCountMax,
            InventoryType.Lapenshard => Constant.BagSlotTabLapenshardCountMax,
            InventoryType.Fragment => Constant.BagSlotTabPieceCountMax,
            _ => throw new ArgumentOutOfRangeException($"Invalid InventoryType: {type}"),
        };
    }

    public void Load() {
        lock (session.Item) {
            foreach ((InventoryType type, ItemCollection items) in tabs) {
                session.Send(ItemInventoryPacket.Reset(type));
                session.Send(ItemInventoryPacket.ExpandCount(type, items.Size - BaseSize(type)));
                // Load items for above tab
                foreach (ImmutableList<Item> batch in items.Batch(BATCH_SIZE)) {
                    session.Send(ItemInventoryPacket.Load(batch));
                }
            }
        }
    }

    public bool Move(long uid, short dstSlot) {
        lock (session.Item) {
            if (dstSlot < 0) {
                session.Send(ItemInventoryPacket.Error(s_item_err_Invalid_slot));
                return false;
            }

            ItemCollection? items = tabs.Values.FirstOrDefault(collection => collection.Contains(uid));
            if (items == null || dstSlot >= items.Size) {
                return false;
            }

            // Attempt to stack
            Item? srcItem = items.Get(uid);
            if (srcItem != null) {
                IList<(Item, int)> results = items.Stack(srcItem, dstSlot);
                if (results.Count > 0) {
                    (Item item, int _) = results.First();
                    if (srcItem.Amount == 0) {
                        items.Remove(uid, out _);
                        Discard(srcItem);

                        session.Send(ItemInventoryPacket.Remove(uid));
                    } else {
                        session.Send(ItemInventoryPacket.UpdateAmount(srcItem.Uid, srcItem.Amount));
                    }
                    session.Send(ItemInventoryPacket.UpdateAmount(item.Uid, item.Amount));

                    return true;
                }
            }

            if (items.Remove(uid, out srcItem)) {
                short srcSlot = srcItem.Slot;
                if (items.RemoveSlot(dstSlot, out Item? removeDst)) {
                    items[srcSlot] = removeDst;
                }

                items[dstSlot] = srcItem;

                session.Send(ItemInventoryPacket.Move(removeDst?.Uid ?? 0, srcSlot, uid, dstSlot));
            }

            return true;
        }
    }

    public bool Add(Item add, bool notifyNew = false, bool commit = false) {
        lock (session.Item) {
            if (add.IsCurrency()) {
                switch (add.Id) {
                    case 90000001:
                    case 90000002:
                    case 90000003:
                        session.Currency.Meso += add.Amount;
                        break;
                    case 90000004: // Meret
                        // case 90000011: // Meret (Secondary)
                        // case 90000015: // GameMeret (Secondary)
                        // case 90000016: // EventMeret (Secondary)
                        // case 90000020: // RedMeret
                        session.Currency.Meret += add.Amount;
                        break;
                    case 90000006: // ValorToken
                        session.Currency[CurrencyType.ValorToken] += add.Amount;
                        break;
                    case 90000008: // ExperienceOrb
                        session.Exp.AddExp(ExpType.expDrop, additionalExp: add.Amount);
                        break;
                    case 90000009: // SpiritOrb
                        break;
                    case 90000010: // StaminaOrb
                        break;
                    case 90000013: // Rue
                        session.Currency[CurrencyType.Rue] += add.Amount;
                        break;
                    case 90000014: // HaviFruit
                        session.Currency[CurrencyType.HaviFruit] += add.Amount;
                        break;
                    case 90000017: // Treva
                        session.Currency[CurrencyType.Treva] += add.Amount;
                        break;
                    case 90000027: // MesoToken
                        session.Currency[CurrencyType.MesoToken] += add.Amount;
                        break;
                    // case 90000005: // DungeonKey
                    // case 90000007: // Karma
                    // case 90000012: // Unknown (BookIcon)
                    // case 90000018: // ShadowFragment
                    // case 90000019: // DistinctPaul
                    case 90000021: // GuildFunds
                    case 90000022: // ReverseCoin
                        session.Currency[CurrencyType.ReverseCoin] += add.Amount;
                        break;
                    case 90000023: // MentorPoint
                        session.Currency[CurrencyType.MentorToken] += add.Amount;
                        break;
                    case 90000024: // MenteePoint
                        session.Currency[CurrencyType.MenteeToken] += add.Amount;
                        break;
                    case 90000025: // StarPoint
                        session.Currency[CurrencyType.StarPoint] += add.Amount;
                        break;
                        // case 90000026: // Unknown (Blank)

                }
                session.Item.Inventory.Discard(add);
                return true;
            }


            if (!tabs.TryGetValue(add.Inventory, out ItemCollection? items)) {
                session.Send(ItemInventoryPacket.Error(s_item_err_not_active_tab));
                return false;
            }

            // If we are adding an item without a Uid, it may need to be created in db.
            if (add.Uid == 0) {
                // Slot MUST be -1 so we don't add directly to a slot.
                add.Slot = -1;
                int remainStack = items.GetStackResult(add);
                if (remainStack > 0) {
                    if (items.OpenSlots <= 0) {
                        return false;
                    }

                    using GameStorage.Request db = session.GameStorage.Context();
                    Item? newAdd = db.CreateItem(session.CharacterId, add);
                    if (newAdd == null) {
                        return false;
                    }

                    add = newAdd;
                }
            }

            if (add.Metadata.Limit.TransferType is TransferType.BindOnLoot) {
                add.Transfer?.Bind(session.Player.Value.Character);
            }

            bool stack = !(add.ExpiryTime > 0);
            IList<(Item, int Added)> result = items.Add(add, stack);
            if (result.Count == 0) {
                session.Send(ItemInventoryPacket.Error(s_err_inventory));
                return false;
            }

            if (add.Amount == 0) {
                Discard(add, commit);
            } else if (commit) {
                using GameStorage.Request db = session.GameStorage.Context();
                db.SaveItems(session.CharacterId, add);
            }

            foreach ((Item item, int added) in result) {
                session.Send(item.Uid == add.Uid
                    ? ItemInventoryPacket.Add(add)
                    : ItemInventoryPacket.UpdateAmount(item.Uid, item.Amount));

                if (notifyNew) {
                    session.Send(ItemInventoryPacket.NotifyNew(item.Uid, added));
                }
                session.ConditionUpdate(ConditionType.item_collect, codeLong: item.Id);
                session.ConditionUpdate(ConditionType.item_add, counter: item.Amount, codeLong: item.Id);
                session.ConditionUpdate(ConditionType.item_exist, counter: item.Amount, codeLong: item.Id);
            }

            return true;
        }
    }

    public bool CanAdd(Item item) {
        lock (session.Item) {
            if (tabs.TryGetValue(item.Inventory, out ItemCollection? items)) {
                return items.OpenSlots > 0 || items.GetStackResult(item) == 0;
            }

            return false;
        }
    }

    public bool Remove(long uid, [NotNullWhen(true)] out Item? removed, int amount = -1) {
        lock (session.Item) {
            return RemoveInternal(uid, amount, out removed);
        }
    }

    public bool Consume(long uid, int amount = -1) {
        lock (session.Item) {
            return ConsumeInternal(uid, amount);
        }
    }

    public bool Consume(ICollection<IngredientInfo> ingredients) {
        lock (session.Item) {
            // Build this index so we don't need to find materials twice.
            Dictionary<ItemTag, IList<Item>> ingredientsByTag = ingredients.ToDictionary(
                entry => entry.Tag,
                entry => Filter(item => item.Metadata.Property.Tag == entry.Tag && !item.IsExpired())
            );

            // Validate
            foreach (IngredientInfo info in ingredients) {
                int remaining = info.Amount;
                foreach (Item ingredient in ingredientsByTag[info.Tag]) {
                    remaining -= ingredient.Amount;
                    if (remaining <= 0) {
                        break;
                    }
                }

                if (remaining > 0) {
                    return false;
                }
            }

            // Consume
            foreach (IngredientInfo info in ingredients) {
                int remaining = info.Amount;
                foreach (Item ingredient in ingredientsByTag[info.Tag]) {
                    int consume = Math.Min(remaining, ingredient.Amount);
                    if (!ConsumeInternal(ingredient.Uid, consume)) {
                        Log.Fatal("Failed to consume ingredient {ItemUid}", ingredient.Uid);
                        throw new InvalidOperationException($"Fatal: Consuming ingredient: {ingredient.Uid}");
                    }

                    remaining -= consume;
                    if (remaining <= 0) {
                        break;
                    }
                }
            }

            return true;
        }
    }

    public bool ConsumeItemComponents(IReadOnlyList<ItemComponent> components, int quantityMultiplier = 1) {
        lock (session.Item) {

            // Check for components
            Dictionary<int, List<Item>> materialsById = components.ToDictionary(
                ingredient => ingredient.ItemId,
                ingredient => session.Item.Inventory.Find(ingredient.ItemId, ingredient.Rarity).ToList()
            );
            var materialsByTag = new Dictionary<ItemTag, IList<Item>>();
            foreach (ItemComponent ingredient in components) {
                if (materialsByTag.TryGetValue(ingredient.Tag, out IList<Item>? value)) {
                    foreach (Item item in session.Item.Inventory.Find(ingredient.ItemId, ingredient.Rarity)) {
                        value.Add(item);
                    }
                } else {
                    materialsByTag.Add(ingredient.Tag, session.Item.Inventory.Find(ingredient.ItemId, ingredient.Rarity).ToList());
                }
            }

            foreach (ItemComponent ingredient in components) {
                int remaining = ingredient.Amount * quantityMultiplier;
                if (ingredient.Tag != ItemTag.None) {
                    foreach (Item material in materialsByTag[ingredient.Tag]) {
                        remaining -= material.Amount;
                        if (remaining <= 0) {
                            break;
                        }
                    }
                } else {
                    foreach (Item material in materialsById[ingredient.ItemId]) {
                        remaining -= material.Amount;
                        if (remaining <= 0) {
                            break;
                        }
                    }
                }

                if (remaining > 0) {
                    return false;
                }
            }

            foreach (ItemComponent ingredient in components) {
                int remainingIngredients = ingredient.Amount * quantityMultiplier;
                if (ingredient.Tag != ItemTag.None) {
                    foreach (Item material in materialsByTag[ingredient.Tag]) {
                        int consume = Math.Min(remainingIngredients, material.Amount);
                        if (!session.Item.Inventory.Consume(material.Uid, consume)) {
                            Log.Fatal("Failed to consume item {ItemUid}", material.Uid);
                            throw new InvalidOperationException($"Fatal: Consuming item: {material.Uid}");
                        }

                        remainingIngredients -= consume;
                        if (remainingIngredients <= 0) {
                            break;
                        }
                    }
                } else {
                    foreach (Item material in materialsById[ingredient.ItemId]) {
                        int consume = Math.Min(remainingIngredients, material.Amount);
                        if (!session.Item.Inventory.Consume(material.Uid, consume)) {
                            Log.Fatal("Failed to consume item {ItemUid}", material.Uid);
                            throw new InvalidOperationException($"Fatal: Consuming item: {material.Uid}");
                        }

                        remainingIngredients -= consume;
                        if (remainingIngredients <= 0) {
                            break;
                        }
                    }
                }
            }
        }
        return true;
    }

    public void Sort(InventoryType type, bool removeExpired = false) {
        lock (session.Item) {
            if (!tabs.TryGetValue(type, out ItemCollection? items)) {
                session.Send(ItemInventoryPacket.Error(s_item_err_not_active_tab));
                return;
            }

            if (removeExpired) {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                IList<Item> toRemove = items.Where(item => item.ExpiryTime <= now).ToList();
                foreach (Item item in toRemove) {
                    if (items.Remove(item.Uid, out Item? removed)) {
                        Discard(removed);
                    }
                }
            }

            items.Sort();

            session.Send(ItemInventoryPacket.Reset(type));
            foreach (ImmutableList<Item> batch in items.Batch(BATCH_SIZE)) {
                session.Send(ItemInventoryPacket.LoadTab(type, batch));
            }
        }
    }

    public void Expand(InventoryType type) {
        lock (session.Item) {
            if (!tabs.TryGetValue(type, out ItemCollection? items)) {
                session.Send(ItemInventoryPacket.Error(s_item_err_not_active_tab));
                return;
            }

            short newExpand = (short) (session.Player.Value.Unlock.Expand[type] + Constant.InventoryExpandRowCount);
            if (newExpand > MaxExpandSize(type)) {
                // There is client side validation for this, but if the server side limits mismatch, use this error.
                session.Send(NoticePacket.MessageBox(StringCode.s_inventory_err_expand_max));
                return;
            }

            if (session.Currency.Meret < Constant.InventoryExpandPrice1Row) {
                session.Send(ItemInventoryPacket.Error(s_cannot_charge_merat));
                return;
            }

            if (!items.Expand((short) (BaseSize(type) + newExpand))) {
                return;
            }

            session.Currency.Meret -= Constant.InventoryExpandPrice1Row;
            if (session.Player.Value.Unlock.Expand.ContainsKey(type)) {
                session.Player.Value.Unlock.Expand[type] = newExpand;
            } else {
                session.Player.Value.Unlock.Expand[type] = Constant.InventoryExpandRowCount;
            }

            session.Send(ItemInventoryPacket.ExpandCount(type, newExpand));
            session.Send(ItemInventoryPacket.ExpandComplete());
        }
    }

    public short FreeSlots(InventoryType type) {
        lock (session.Item) {
            return !tabs.TryGetValue(type, out ItemCollection? items) ? (short) 0 : items.OpenSlots;
        }
    }

    public Item? Get(long uid, InventoryType? type = null) {
        lock (session.Item) {
            if (type != null) {
                return tabs[(InventoryType) type].Get(uid);
            }

            return tabs.Values.FirstOrDefault(collection => collection.Contains(uid))?.Get(uid);
        }
    }

    public IList<Item> Filter(Func<Item, bool> condition, InventoryType? type = null) {
        lock (session.Item) {
            if (type != null) {
                return tabs[(InventoryType) type].Where(condition).ToList();
            }

            return tabs.Values.SelectMany(tab => tab.Where(condition)).ToList();
        }
    }

    public IEnumerable<Item> Find(int id, int rarity = -1) {
        lock (session.Item) {
            if (!session.ItemMetadata.TryGet(id, out ItemMetadata? metadata)) {
                yield break;
            }

            InventoryType type = metadata.Inventory();
            if (!tabs.TryGetValue(type, out ItemCollection? items)) {
                session.Send(ItemInventoryPacket.Error(s_item_err_not_active_tab));
                yield break;
            }

            foreach (Item item in items) {
                if (item.Id != id) continue;
                if (rarity != -1 && item.Rarity != rarity) continue;

                yield return item;
            }
        }
    }

    #region Internal (No Locks)
    private bool RemoveInternal(long uid, int amount, [NotNullWhen(true)] out Item? removed) {
        ItemCollection? items = tabs.Values.FirstOrDefault(collection => collection.Contains(uid));
        if (items == null || amount == 0) {
            removed = null;
            return false;
        }

        if (amount > 0) {
            Item? item = items.Get(uid);
            if (item == null || item.Amount < amount) {
                session.Send(ItemInventoryPacket.Error(s_item_err_invalid_count));
                removed = null;
                return false;
            }

            // Otherwise, we would just do a full remove.
            if (item.Amount > amount) {
                using GameStorage.Request db = session.GameStorage.Context();
                removed = db.SplitItem(0, item, amount);
                if (removed == null) {
                    return false;
                }
                item.Amount -= amount;

                session.Send(ItemInventoryPacket.UpdateAmount(uid, item.Amount));
                return true;
            }
        }

        // Full remove of item
        if (items.Remove(uid, out removed)) {
            session.Send(ItemInventoryPacket.Remove(uid));
            return true;
        }

        return false;
    }

    private bool ConsumeInternal(long uid, int amount, bool commit = false) {
        ItemCollection? items = tabs.Values.FirstOrDefault(collection => collection.Contains(uid));
        if (items == null || amount == 0) {
            return false;
        }

        if (amount > 0) {
            Item? item = items.Get(uid);
            if (item == null || item.IsExpired() || item.Amount < amount) {
                return false;
            }

            // Otherwise, we would just do a full remove.
            if (item.Amount > amount) {
                item.Amount -= amount;

                session.Send(ItemInventoryPacket.UpdateAmount(uid, item.Amount));
                return true;
            }
        }

        // Full remove of item
        if (items.Remove(uid, out Item? removed)) {
            Discard(removed, commit);
            session.Send(ItemInventoryPacket.Remove(uid));
            return true;
        }

        return false;
    }
    #endregion

    public void Discard(Item item, bool commit = false) {
        // Only discard items that need to be saved to DB.
        if (item.Uid == 0) {
            return;
        }

        if (commit) {
            lock (session.Item) {
                using GameStorage.Request db = session.GameStorage.Context();
                db.SaveItems(0, item);
            }
        } else {
            delete.Add(item);
        }
    }

    public void Save(GameStorage.Request db) {
        lock (session.Item) {
            db.SaveItems(0, delete.ToArray());
            foreach (ItemCollection tab in tabs.Values) {
                db.SaveItems(session.CharacterId, tab.ToArray());
            }
        }
    }
}
