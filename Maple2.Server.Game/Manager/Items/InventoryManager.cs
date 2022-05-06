using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Core.Constants;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Microsoft.Extensions.Logging;
using static Maple2.Model.Error.ItemInventoryError;

namespace Maple2.Server.Game.Manager.Items;

public class InventoryManager {
    private const int BATCH_SIZE = 10;
    private const int EXPAND_SLOTS = 6;

    private readonly GameSession session;
    private readonly ReaderWriterLockSlim mutex;

    private readonly Dictionary<InventoryType, ItemCollection> tabs;
    private readonly List<Item> delete;

    public InventoryManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        mutex = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        tabs = new Dictionary<InventoryType, ItemCollection>();
        foreach (InventoryType type in Enum.GetValues<InventoryType>()) {
            tabs[type] = new ItemCollection(BaseSize(type));
        }

        delete = new List<Item>();
        foreach ((InventoryType type, List<Item> load) in db.GetInventory(session.CharacterId)) {
            if (tabs.TryGetValue(type, out ItemCollection? items)) {
                foreach (Item item in load) {
                    if (items.Add(item).Count == 0) {
                        session.Log(LogLevel.Error, "Failed to add item:{Uid}", item.Uid);
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

    public void Load() {
        mutex.EnterReadLock();
        try {
            foreach ((InventoryType type, ItemCollection items) in tabs) {
                session.Send(ItemInventoryPacket.Reset(type));
                session.Send(ItemInventoryPacket.ExpandCount(type, items.Size - BaseSize(type)));
                // Load items for above tab
                foreach (ImmutableList<Item> batch in items.Batch(BATCH_SIZE)) {
                    session.Send(ItemInventoryPacket.Load(batch));
                }
            }
        } finally {
            mutex.ExitReadLock();
        }
    }

    public bool Move(long uid, short dstSlot) {
        if (dstSlot < 0) {
            session.Send(ItemInventoryPacket.Error(s_item_err_Invalid_slot));
            return false;
        }

        mutex.EnterWriteLock();
        try {
            ItemCollection? items = GetTab(uid);
            if (items == null || dstSlot >= items.Size) {

                return false;
            }

            if (items.Remove(uid, out Item? removed)) {
                short srcSlot = removed!.Slot;
                Item? existing = items[dstSlot];
                items[dstSlot] = removed;
                items[srcSlot] = existing;

                session.Send(ItemInventoryPacket.Move(existing?.Uid ?? 0, srcSlot, uid, dstSlot));
            }

            return true;
        } finally {
            mutex.ExitWriteLock();
        }
    }

    public bool Add(Item add, bool notifyNew = false) {
        mutex.EnterWriteLock();
        try {
            if (!tabs.TryGetValue(add.Inventory, out ItemCollection? items)) {
                session.Send(ItemInventoryPacket.Error(s_item_err_not_active_tab));
                return false;
            }

            IList<(Item, int Added)> result = items.Add(add, true);
            if (result.Count == 0) {
                session.Send(ItemInventoryPacket.Error(s_err_inventory));
                return false;
            }

            // Item was stacked onto existing slots.
            if (add.Amount == 0) {
                delete.Add(add);
            }

            foreach ((Item item, int added) in result) {
                session.Send(item.Uid == add.Uid
                    ? ItemInventoryPacket.Add(add)
                    : ItemInventoryPacket.UpdateAmount(item.Uid, item.Amount));

                if (notifyNew) {
                    session.Send(ItemInventoryPacket.NotifyNew(item.Uid, added));
                }
            }

            return true;
        } finally {
            mutex.ExitWriteLock();
        }
    }

    public bool Remove(long uid, out Item? removed, int amount = -1) {
        mutex.EnterWriteLock();
        try {
            return RemoveInternal(uid, amount, out removed);
        } finally {
            mutex.ExitWriteLock();
        }
    }

    public void Sort(InventoryType type, bool removeExpired = false) {
        if (!tabs.TryGetValue(type, out ItemCollection? items)) {
            session.Send(ItemInventoryPacket.Error(s_item_err_not_active_tab));
            return;
        }

        if (removeExpired) {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            IList<Item> toRemove = items.Where(item => item.ExpiryTime <= now).ToList();
            foreach (Item item in toRemove) {
                if (items.Remove(item.Uid, out Item? removed)) {
                    delete.Add(removed!);
                }
            }
        }

        items.Sort();

        session.Send(ItemInventoryPacket.Reset(type));
        foreach (ImmutableList<Item> batch in items.Batch(BATCH_SIZE)) {
            session.Send(ItemInventoryPacket.LoadTab(type, batch));
        }
    }

    public void Expand(InventoryType type) {
        if (!tabs.TryGetValue(type, out ItemCollection? items)) {
            session.Send(ItemInventoryPacket.Error(s_item_err_not_active_tab));
            return;
        }

        //session.Send(ItemInventoryPacket.Error(s_cannot_charge_merat));

        if (items.Expand((short) (items.Size + EXPAND_SLOTS))) {
            session.Send(ItemInventoryPacket.ExpandCount(type, items.Size - BaseSize(type)));
            session.Send(ItemInventoryPacket.ExpandComplete());
        }
    }

    // Just iterating instead of creating another map.
    // Shouldn't really matter since there's <15 tabs.
    public ItemCollection? GetTab(long uid) {
        mutex.EnterReadLock();
        try {
            foreach (ItemCollection items in tabs.Values) {
                if (items.Contains(uid)) {
                    return items;
                }
            }
        } finally {
            mutex.ExitReadLock();
        }

        return null;
    }

    public Item? Get(long uid) {
        return GetTab(uid)?.Get(uid);
    }

    public IEnumerable<Item> Find(int id, int rarity = -1) {
        InventoryType type = session.ItemMetadata.Get(id).Inventory();
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

    #region Internal (No Locks)
    private bool RemoveInternal(long uid, int amount, out Item? removed) {
        ItemCollection? items = GetTab(uid);
        if (items == null || amount == 0) {
            removed = null;
            return false;
        }

        if (amount > 0) {
            Item? item = items.Get(uid);
            if (item == null || item.Amount < amount) {
                removed = null;
                return false;
            }

            // Otherwise, we would just do a full remove.
            if (item.Amount > amount) {
                using GameStorage.Request db = session.GameStorage.Context();
                removed = db.SplitItem(0, item, amount);
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
    #endregion

    public void Save(GameStorage.Request db) {
        db.SaveItems(0, delete.ToArray());
        foreach (ItemCollection tab in tabs.Values) {
            db.SaveItems(session.CharacterId, tab.ToArray());
        }
    }
}
