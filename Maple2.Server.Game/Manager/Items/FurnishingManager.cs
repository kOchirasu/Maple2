using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Serilog;

namespace Maple2.Server.Game.Manager.Items;

public class FurnishingManager {
    private readonly GameSession session;

    private readonly ItemCollection storage;

    public FurnishingManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        storage = new ItemCollection(Constant.FurnishingStorageMaxSlot);

        List<Item>? items = db.GetItemGroups(session.AccountId, ItemGroup.Furnishing)
            .GetValueOrDefault(ItemGroup.Furnishing);
        if (items == null) {
            return;
        }

        foreach (Item item in items) {
            if (storage.Add(item).Count == 0) {
                Log.Error("Failed to add furnishing:{Uid}", item.Uid);
            }
        }
    }

    public void Load() {
        lock (session.Item) {
            Item[] items = storage.Where(item => item.Amount > 0).ToArray();
            session.Send(FurnishingStoragePacket.Count(items.Length));
            session.Send(FurnishingStoragePacket.StartList());
            foreach (Item item in items) {
                session.Send(FurnishingStoragePacket.Add(item));
            }
            session.Send(FurnishingStoragePacket.EndList());
        }
    }

    /// <summary>
    /// Withdraws <param>amount</param> from the specified item uid.
    /// If there are no amount remaining, we still keep the entry to allow reuse of the item uid.
    /// </summary>
    /// <param name="uid">Uid of the item to withdraw from</param>
    /// <returns>Information about the withdrawn cube</returns>
    public UgcItemCube? Withdraw(long uid) {
        const int amount = 1; // We always withdraw 1 cube

        lock (session.Item) {
            Item? item = storage.Get(uid);
            if (item == null || item.Amount < amount) {
                return null;
            }

            // We do not remove item from inventory even if it hits 0
            // this allows us to preserve the uid for later.
            item.Amount -= amount;
            session.Send(item.Amount > 0
                ? FurnishingStoragePacket.Update(item.Uid, item.Amount)
                : FurnishingStoragePacket.Remove(item.Uid));

            return new UgcItemCube(item.Id, item.Uid, item.Template);
        }
    }

    /// <summary>
    /// Deposits <param>amount</param> onto item with the specified item uid.
    /// This solely increments the amount and will not generate any new items in the database.
    /// </summary>
    /// <param name="uid">Uid of the item to stack on</param>
    /// <param name="amount">Amount of the item to deposit</param>
    /// <returns>Whether or not the deposit was successful</returns>
    public bool Deposit(long uid, int amount = 1) {
        lock (session.Item) {
            Item? item = storage.Get(uid);
            if (item == null || amount <= 0) {
                return false;
            }

            bool added = item.Amount <= 0;
            item.Amount = Math.Clamp(item.Amount + amount, amount, item.Metadata.Property.SlotMax);
            session.Send(added
                ? FurnishingStoragePacket.Add(item)
                : FurnishingStoragePacket.Update(item.Uid, item.Amount));
            return true;
        }
    }

    public bool Deposit(Item item) {
        lock (session.Item) {
            // We already have this item to create
            Item? stored = storage.FirstOrDefault(existing => existing.Id == item.Id);
            if (stored != null) {
                return Deposit(stored.Uid, item.Amount);
            }

            if (storage.OpenSlots < 1) {
                return false;
            }

            using GameStorage.Request db = session.GameStorage.Context();
            Item? created = db.CreateItem(session.AccountId, item);
            if (created == null || storage.Add(created).Count <= 0) {
                return false;
            }

            FurnishingStoragePacket.Add(created);
            return true;
        }
    }

    public UgcItemCube? Get(long uid) {
        lock (session.Item) {
            Item? item = storage.Get(uid);
            if (item == null) {
                return null;
            }

            return new UgcItemCube(item.Id, item.Uid, item.Template);
        }
    }

    public void Save(GameStorage.Request db) {
        lock (session.Item) {
            db.SaveItems(session.AccountId, storage.ToArray());
        }
    }
}
