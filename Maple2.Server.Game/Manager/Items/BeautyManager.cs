using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Serilog;
using static Maple2.Model.Error.StorageInventoryError;

namespace Maple2.Server.Game.Manager.Items;

public sealed class BeautyManager : IDisposable {
    private readonly GameSession session;
    private readonly ItemCollection items;
    private short expand;
    private Item? previousHair;
    public BeautyManager(GameSession session) {
        this.session = session;
        items = new ItemCollection(Constant.BaseStorageCount);

        using GameStorage.Request db = session.GameStorage.Context();
        foreach (Item item in db.GetSavedHairs(session.CharacterId)) {
            if (items.Add(item).Count == 0) {
                Log.Error("Failed to add saved hair:{Uid}", item.Uid);
            }
        }
    }

    public void Dispose() {
        using GameStorage.Request db = session.GameStorage.Context();
        lock (session.Item) {
            db.SaveItems(session.CharacterId, items.ToArray());

            session.Beauty = null;
        }
    }

    public void Load() {
        lock (session.Item) {
            session.Send(BeautyPacket.StartList());
            session.Send(BeautyPacket.SaveSlots(expand));
            session.Send(BeautyPacket.ListCount(items.Count));
            if (items.Count > 0) {
                session.Send(BeautyPacket.ListHair(items.OrderBy(hair => hair.CreationTime).ToList()));;
            }
        }
    }

    public void Add(long uid) {
        Item? hair = session.Item.GetOutfit(uid);
        if (hair == null) {
            return;
        }

        if (items.Any(savedHair => savedHair.Uid == uid)) {
            return;
        }

        if (items.Count >= Constant.HairSlotCount + expand) {
            session.Send(BeautyPacket.Error(BeautyError.s_beauty_msg_error_style_slot_max));
            return;
        }

        Item? hairCopy = hair.Clone();
        hairCopy.Group = ItemGroup.SavedHair;
        using GameStorage.Request db = session.GameStorage.Context();
        hairCopy = db.CreateItem(session.CharacterId, hairCopy);
        if (hairCopy == null) {
            return;
        }
        items.Add(hairCopy);
        session.Send(BeautyPacket.SaveHair(hair, hairCopy));
    }

    public bool Remove(long uid) {
        if (!items.Remove(uid, out Item? hair)) {
            return false;
        }
        
        session.Item.Inventory.Discard(hair);
        session.Send(BeautyPacket.DeleteHair(uid));
        return true;
    }

    public void SavePreviousHair(Item? prevHair) => previousHair = prevHair;

    public void SelectPreviousHair() {
        if (previousHair == null) {
            return;
        }
        session.Item.Equips.EquipCosmetic(previousHair, EquipSlot.HR);
    }
    public void ClearPreviousHair() => previousHair = null;

    public bool EquipCosmetic(long uid) {
        Item? cosmetic = items.Get(uid);
        if (cosmetic == null) {
            return false;
        }

        // invalid cosmetic
        if (cosmetic.Metadata.SlotNames.Length > 1) {
            return false;
        }

        Item? copy = cosmetic.Clone();
        copy.Group = ItemGroup.Default;
            
        using GameStorage.Request db = session.GameStorage.Context();
        copy = db.CreateItem(session.CharacterId, copy);
        if (copy == null) {
            return false;
        }

        if (!session.Item.Equips.EquipCosmetic(copy, copy.Metadata.SlotNames[0])) {
            return false;
        }
        session.Send(BeautyPacket.ApplySavedHair());
        return true;
    }

    public void Expand() {
        lock (session.Item) {
            short newSize = (short) (items.Size + Constant.InventoryExpandRowCount);
            if (newSize > Constant.StoreExpandMaxSlotCount) {
                session.Send(StorageInventoryPacket.Error(s_store_err_expand_max));
                return;
            }
            if (session.Currency.Meret < Constant.StoreExpandPrice1Row) {
                session.Send(StorageInventoryPacket.Error(s_cannot_charge_merat));
                return;
            }

            if (!items.Expand(newSize)) {
                session.Send(StorageInventoryPacket.Error(s_store_err_code));
                return;
            }

            session.Currency.Meret -= Constant.StoreExpandPrice1Row;
            expand += Constant.InventoryExpandRowCount;

            Load();
        }
    }

    #region Internal (No Locks)
    private bool RemoveInternal(long uid, int amount, [NotNullWhen(true)] out Item? removed) {
        if (amount > 0) {
            Item? item = items.Get(uid);
            if (item == null || item.Amount < amount) {
                session.Send(StorageInventoryPacket.Error(s_item_err_invalid_count));
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

                session.Send(StorageInventoryPacket.Update(uid, item.Amount));
                return true;
            }
        }

        // Full remove of item
        if (items.Remove(uid, out removed)) {
            session.Send(StorageInventoryPacket.Remove(uid));
            return true;
        }

        return false;
    }
    #endregion
}
