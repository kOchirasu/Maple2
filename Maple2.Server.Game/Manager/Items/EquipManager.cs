using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Items;

public class EquipManager {
    private readonly GameSession session;

    public readonly ConcurrentDictionary<EquipSlot, Item> Gear;
    public readonly ConcurrentDictionary<EquipSlot, Item> Outfit;
    public readonly ConcurrentDictionary<BadgeType, Item> Badge;

    public EquipManager(GameStorage.Request db, GameSession session) {
        this.session = session;

        Gear = new ConcurrentDictionary<EquipSlot, Item>();
        Outfit = new ConcurrentDictionary<EquipSlot, Item>();
        Badge = new ConcurrentDictionary<BadgeType, Item>();

        // Load from DB
        foreach ((EquipTab tab, List<Item> items) in db.GetEquips(session.CharacterId, EquipTab.Gear, EquipTab.Outfit, EquipTab.Badge)) {
            foreach (Item item in items) {
                switch (tab) {
                    case EquipTab.Gear:
                        Gear[item.EquipSlot] = item;
                        break;
                    case EquipTab.Outfit:
                        Outfit[item.EquipSlot] = item;
                        break;
                    case EquipTab.Badge:
                        if (item.Badge != null) {
                            Badge[item.Badge.Type] = item;
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Equips an item to its specified slot
    /// </summary>
    /// <param name="itemUid">The uid of the item to be equipped</param>
    /// <param name="slot">The slot to be equipped to.</param>
    /// <param name="isSkin">Whether the item is a skin or not.</param>
    /// <returns>The items if any that were unequipped to equip this item.</returns>
    public bool Equip(long itemUid, EquipSlot slot, bool isSkin) {
        // Check that item is valid and can be equipped.
        Item? item = session.Item.Inventory.Get(itemUid);
        if (item == null) {
            return false;
        }

        Debug.Assert(item.Metadata.Property.IsSkin == isSkin);

        if (!ValidEquipSlotForItem(slot, item)) {
            throw new InvalidOperationException($"Cannot equip item {item.Id} to slot {slot}");
        }

        ConcurrentDictionary<EquipSlot, Item> equips = isSkin ? Outfit : Gear;
        if (item.Metadata.SlotNames.Length > 1) { // Two-Handed Weapon, Overall Armor
            int requiredFreeSlots = 0;
            foreach (EquipSlot removeSlot in item.Metadata.SlotNames) {
                if (equips.ContainsKey(removeSlot)) {
                    requiredFreeSlots++;
                }
            }

            // +1 because we will have a free slot from the item being equipped.
            int freeSlots = session.Item.Inventory.FreeSlots(isSkin ? InventoryType.Outfit : InventoryType.Gear) + 1;
            if (freeSlots <= requiredFreeSlots) {
                return false;
            }
        }

        // Remove item being equipped from inventory so unequipped items and be moved there.
        if (!session.Item.Inventory.Remove(itemUid, out item)) {
            return false;
        }

        if (!UnequipInternal(slot, isSkin, item.Slot)) {
            throw new InvalidOperationException("Failed to unequip item");
        }
        if (item.Metadata.SlotNames.Length > 1) { // Two-Handed Weapon, Overall Armor
            foreach (EquipSlot unequipSlot in item.Metadata.SlotNames) {
                if (!UnequipInternal(unequipSlot, isSkin)) {
                    throw new InvalidOperationException("Failed to unequip item");
                }
            }
        }

        item.EquipTab = isSkin ? EquipTab.Outfit : EquipTab.Gear;
        item.EquipSlot = slot;
        item.Slot = -1;
        equips[slot] = item;
        session.Field?.Multicast(EquipPacket.EquipItem(session.Player, item, 0));

        return true;
    }

    /// <summary>
    /// Unequips an item.
    /// </summary>
    /// <param name="itemUid">The uid of the item to be unequipped</param>
    /// <returns></returns>
    public bool Unequip(long itemUid) {
        // Unequip from Gear.
        foreach ((EquipSlot slot, Item item) in Gear) {
            if (itemUid == item.Uid) {
                return UnequipInternal(slot, false);
            }
        }

        // Unequip from Outfit.
        foreach ((EquipSlot slot, Item item) in Outfit) {
            if (itemUid == item.Uid) {
                return UnequipInternal(slot, true);
            }
        }

        return false;
    }

    public bool EquipBadge(long itemUid) {
        Item? item = session.Item.Inventory.Get(itemUid);
        if (item?.Badge == null) {
            return false;
        }

        // Remove item being equipped from inventory so unequipped items and be moved there.
        if (!session.Item.Inventory.Remove(itemUid, out item) || item.Badge == null) {
            return false;
        }

        if (!UnequipBadge(item.Badge.Type, item.Slot)) {
            return false;
        }

        item.EquipTab = EquipTab.Badge;
        item.EquipSlot = EquipSlot.Unknown;
        item.Slot = -1;
        session.Field?.Multicast(EquipPacket.EquipBadge(session.Player, item));

        return true;
    }

    public bool UnequipBadge(BadgeType slot, short inventorySlot = -1) {
        if (!Badge.TryRemove(slot, out Item? unequipItem)) {
            return true;
        }

        if (unequipItem.Badge == null) {
            throw new InvalidOperationException("Unequipped badge that is not a badge");
        }

        unequipItem.EquipTab = EquipTab.None;
        unequipItem.EquipSlot = EquipSlot.Unknown;
        unequipItem.Slot = inventorySlot;
        bool success = session.Item.Inventory.Add(unequipItem);

        if (success) {
            session.Field?.Multicast(EquipPacket.UnequipBadge(session.Player, unequipItem.Badge.Type));
        }
        return success;
    }

    private bool UnequipInternal(EquipSlot slot, bool isSkin, short inventorySlot = -1) {
        ConcurrentDictionary<EquipSlot, Item> equips = isSkin ? Outfit : Gear;
        if (!equips.TryRemove(slot, out Item? unequipItem)) {
            return true;
        }

        unequipItem.EquipTab = EquipTab.None;
        unequipItem.EquipSlot = EquipSlot.Unknown;

        bool success;
        if (slot is EquipSlot.HR or EquipSlot.ER or EquipSlot.FA or EquipSlot.FD) {
            session.Item.Inventory.Discard(unequipItem);
            success = true;
        } else {
            unequipItem.Slot = inventorySlot;
            success = session.Item.Inventory.Add(unequipItem);
        }

        if (success) {
            session.Field?.Multicast(EquipPacket.UnequipItem(session.Player, unequipItem));
        }
        return success;
    }

    private static bool ValidEquipSlot(EquipSlot slot) {
        return slot is not (EquipSlot.SK or EquipSlot.OH or EquipSlot.ER or EquipSlot.Unknown);
    }

    private static bool ValidEquipSlotForItem(EquipSlot slot, Item item) {
        if (!ValidEquipSlot(slot) || item.Metadata.SlotNames.Length == 0) {
            return false;
        }

        // LH and RH are both valid for OH equip.
        if (item.Metadata.SlotNames.Contains(EquipSlot.OH)) {
            return slot is EquipSlot.LH or EquipSlot.RH;
        }

        return slot == item.Metadata.SlotNames[0];
    }

    public void Save(GameStorage.Request db) {
        db.SaveItems(session.CharacterId, Gear.Values.ToArray());
        db.SaveItems(session.CharacterId, Outfit.Values.ToArray());
        db.SaveItems(session.CharacterId, Badge.Values.ToArray());
    }
}
