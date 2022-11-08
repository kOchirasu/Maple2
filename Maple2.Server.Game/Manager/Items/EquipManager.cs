﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Core.Packets;
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
        foreach ((ItemGroup tab, List<Item> items) in db.GetItemGroups(session.CharacterId, ItemGroup.Gear, ItemGroup.Outfit, ItemGroup.Badge)) {
            foreach (Item item in items) {
                switch (tab) {
                    case ItemGroup.Gear:
                        Gear[item.EquipSlot()] = item;
                        break;
                    case ItemGroup.Outfit:
                        Outfit[item.EquipSlot()] = item;
                        break;
                    case ItemGroup.Badge:
                        if (item.Badge != null) {
                            Badge[item.Badge.Type] = item;
                        }
                        break;
                }
            }
        }
    }

    public Item? Get(long itemUid) {
        return Gear.Values.FirstOrDefault(item => item.Uid == itemUid)
               ?? Outfit.Values.FirstOrDefault(item => item.Uid == itemUid)
               ?? Badge.Values.FirstOrDefault(item => item.Uid == itemUid);
    }

    /// <summary>
    /// Equips an item to its specified slot
    /// </summary>
    /// <param name="itemUid">The uid of the item to be equipped</param>
    /// <param name="slot">The slot to be equipped to.</param>
    /// <param name="isSkin">Whether the item is a skin or not.</param>
    /// <returns>The items if any that were unequipped to equip this item.</returns>
    public bool Equip(long itemUid, EquipSlot slot, bool isSkin) {
        lock (session.Item) {
            // Check that item is valid and can be equipped.
            Item? item = session.Item.Inventory.Get(itemUid);
            if (item == null) {
                session.Send(NoticePacket.MessageBox(StringCode.s_item_invalid_do_not_have));
                return false;
            }
            if (item.IsExpired()) {
                session.Send(NoticePacket.MessageBox(StringCode.s_item_err_puton_expired));
                return false;
            }
            if (item.Binding != null && item.Binding.CharacterId != session.CharacterId) {
                session.Send(NoticePacket.MessageBox(StringCode.s_item_err_puton_invalid_binding));
                return false;
            }

            if (item.Metadata.Property.IsSkin != isSkin) {
                return false;
            }
            if (!ValidEquipSlotForItem(slot, item)) {
                return false;
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

            if (item.Metadata.Limit.TransferType == TransferType.BindOnEquip) {
                session.Item.Bind(item);
            }

            item.Group = isSkin ? ItemGroup.Outfit : ItemGroup.Gear;
            item.Slot = (short) slot;
            equips[slot] = item;
            session.Field?.Broadcast(EquipPacket.EquipItem(session.Player, item, 0));

            return true;
        }
    }

    /// <summary>
    /// Unequips an item.
    /// </summary>
    /// <param name="itemUid">The uid of the item to be unequipped</param>
    /// <returns></returns>
    public bool Unequip(long itemUid) {
        lock (session.Item) {
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
    }

    public bool EquipBadge(long itemUid) {
        lock (session.Item) {
            Item? item = session.Item.Inventory.Get(itemUid);
            if (item == null) {
                session.Send(NoticePacket.MessageBox(StringCode.s_item_invalid_do_not_have));
                return false;
            }
            if (item.IsExpired()) {
                session.Send(NoticePacket.MessageBox(StringCode.s_item_err_puton_expired));
                return false;
            }

            // Remove item being equipped from inventory so unequipped items and be moved there.
            if (!session.Item.Inventory.Remove(itemUid, out item) || item.Badge == null) {
                return false;
            }

            if (!UnequipBadge(item.Badge.Type, item.Slot)) {
                return false;
            }

            if (item.Metadata.Limit.TransferType == TransferType.BindOnEquip) {
                session.Item.Bind(item);
            }

            item.Group = ItemGroup.Default;
            item.Slot = -1;
            session.Field?.Broadcast(EquipPacket.EquipBadge(session.Player, item));

            if (item.Badge.Type == BadgeType.PetSkin) {
                session.Pet?.BadgeChanged(item.Badge);
            }

            return true;
        }
    }

    public bool UnequipBadge(BadgeType slot, short inventorySlot = -1) {
        lock (session.Item) {
            if (!Badge.TryRemove(slot, out Item? unequipItem)) {
                return true;
            }

            if (unequipItem.Badge == null) {
                throw new InvalidOperationException("Unequipped badge that is not a badge");
            }

            unequipItem.Group = ItemGroup.Default;
            unequipItem.Slot = inventorySlot;
            bool success = session.Item.Inventory.Add(unequipItem);

            if (success) {
                session.Field?.Broadcast(EquipPacket.UnequipBadge(session.Player, unequipItem.Badge.Type));

                if (unequipItem.Badge.Type == BadgeType.PetSkin) {
                    session.Pet?.BadgeChanged(null);
                }
            }

            return success;
        }
    }

    private bool UnequipInternal(EquipSlot slot, bool isSkin, short inventorySlot = -1) {
        ConcurrentDictionary<EquipSlot, Item> equips = isSkin ? Outfit : Gear;
        if (!equips.TryRemove(slot, out Item? unequipItem)) {
            return true;
        }

        unequipItem.Group = ItemGroup.Default;
        unequipItem.Slot = inventorySlot;

        bool success;
        if (slot is EquipSlot.HR or EquipSlot.ER or EquipSlot.FA or EquipSlot.FD) {
            session.Item.Inventory.Discard(unequipItem);
            success = true;
        } else {
            success = session.Item.Inventory.Add(unequipItem);
        }

        if (success) {
            session.Field?.Broadcast(EquipPacket.UnequipItem(session.Player, unequipItem));
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
        lock (session.Item) {
            db.SaveItems(session.CharacterId, Gear.Values.ToArray());
            db.SaveItems(session.CharacterId, Outfit.Values.ToArray());
            db.SaveItems(session.CharacterId, Badge.Values.ToArray());
        }
    }
}
