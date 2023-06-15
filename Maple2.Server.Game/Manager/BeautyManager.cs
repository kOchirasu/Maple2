using System;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Items;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Serilog;

namespace Maple2.Server.Game.Manager;

public sealed class BeautyManager : IDisposable {
    private readonly GameSession session;
    private readonly ItemCollection items;
    private short expand;
    private Item? previousHair;
    public BeautyManager(GameSession session) {
        this.session = session;
        items = new ItemCollection(Constant.BaseStorageCount);
        using GameStorage.Request db = session.GameStorage.Context();
        expand = db.GetHairStorageAmount(session.CharacterId);
        foreach (Item item in db.GetSavedHairs(session.CharacterId).Where(item => items.Add(item).Count == 0)) {
            Log.Error("Failed to add saved hair:{Uid}", item.Uid);
        }
    }

    public void Dispose() {
        using GameStorage.Request db = session.GameStorage.Context();
        lock (session.Item) {
            db.SaveItems(session.CharacterId, items.ToArray());
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

    public bool EquipSavedCosmetic(long uid) {
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
}
