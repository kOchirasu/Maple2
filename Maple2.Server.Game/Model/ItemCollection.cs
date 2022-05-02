using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Maple2.Model.Game;

namespace Maple2.Server.Game.Model;

/// <remarks>
/// This class is thread-safe.
/// </remarks>
public class ItemCollection : IEnumerable<Item> {
    private readonly ReaderWriterLockSlim mutex;
    private readonly IDictionary<long, short> uidToSlot;
    private Item?[] items;

    public short OpenSlots => (short) (Size - Count);
    public short Size => (short) items.Length;
    public short Count { get; private set; }

    public ItemCollection(short size) {
        mutex = new ReaderWriterLockSlim();
        uidToSlot = new Dictionary<long, short>();
        items = new Item[size];

        Count = 0;
    }

    public Item? this[short slot] {
        get {
            mutex.EnterReadLock();
            try {
                if (slot < 0 || slot >= Size) {
                    return null;
                }

                return items[slot];
            } finally {
                mutex.ExitReadLock();
            }
        }
        set {
            // Setting slot to null or Slot is already taken
            // Use `RemoveSlot()` to remove items.
            if (value == null || this[slot] != null) {
                return;
            }

            mutex.EnterUpgradeableReadLock();
            try {
                Debug.Assert(!uidToSlot.ContainsKey(value.Id), $"Item already in collection:{value.Id}");

                mutex.EnterWriteLock();
                try {
                    value.Slot = slot;
                    uidToSlot[value.Id] = slot;
                    items[slot] = value;
                    Count++;
                } finally {
                    mutex.ExitWriteLock();
                }
            } finally {
                mutex.ExitUpgradeableReadLock();
            }
        }
    }

    public Item? Get(long uid) {
        short slot;
        mutex.EnterReadLock();
        try {
            if (!uidToSlot.TryGetValue(uid, out slot)) {
                return null;
            }
        } finally {
            mutex.ExitReadLock();
        }

        return this[slot];
    }

    public bool Contains(long uid) {
        mutex.EnterReadLock();
        try {
            return uidToSlot.ContainsKey(uid);
        } finally {
            mutex.ExitReadLock();
        }
    }

    public bool Remove(long uid, out Item? removed) {
        mutex.EnterUpgradeableReadLock();
        try {
            if (!uidToSlot.TryGetValue(uid, out short slot)) {
                removed = null;
                return false;
            }

            return RemoveSlot(slot, out removed);
        } finally {
            mutex.ExitUpgradeableReadLock();
        }
    }

    public bool RemoveSlot(short slot, out Item? removed) {
        removed = this[slot];
        if (removed == null) {
            return false;
        }

        mutex.EnterWriteLock();
        try {
            items[slot] = null;
            uidToSlot.Remove(removed.Uid);
            return true;
        } finally {
            mutex.ExitWriteLock();
        }
    }

    public void Sort() {
        mutex.EnterWriteLock();
        try {
            Array.Sort(items, SortItem);

            // Update the slot mapping
            uidToSlot.Clear();
            short i = 0;
            while (items[i] != null) {
                uidToSlot[items[i]!.Id] = i;
                i++;
            }
        } finally {
            mutex.ExitWriteLock();
        }
    }

    public bool Expand(short newSize) {
        if (newSize <= Size) {
            return false;
        }

        mutex.EnterWriteLock();
        try {
            short diff = (short) (newSize - Size);
            Array.Resize(ref items, newSize);
            return true;
        } finally {
            mutex.ExitWriteLock();
        }
    }

    // Enumerate array while ignoring nulls
    public IEnumerator<Item> GetEnumerator() {
        mutex.EnterReadLock();
        try {
            foreach (Item? item in items) {
                if (item == null) continue;
                yield return item;
            }
        } finally {
            mutex.ExitReadLock();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    private static int SortItem(Item? x, Item? y) {
        if (x == null) return 1;
        if (y == null) return -1;

        int result = x.Id.CompareTo(y.Id);
        if (result != 0) return result;
        result = x.Rarity.CompareTo(y.Rarity);
        if (result != 0) return result;
        return x.Amount.CompareTo(y.Amount);
    }
}
