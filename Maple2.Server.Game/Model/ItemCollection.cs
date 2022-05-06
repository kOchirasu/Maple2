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
            if (value == null || slot < 0 || slot >= Size || this[slot] != null) {
                return;
            }

            mutex.EnterUpgradeableReadLock();
            try {
                mutex.EnterWriteLock();
                try {
                    value.Slot = slot;
                    uidToSlot[value.Uid] = slot;
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

    public IList<(Item, int Added)> Add(Item add, bool stack = false) {
        if (!CanHold(add, stack)) {
            return Array.Empty<(Item, int)>();
        }

        // Prefer item slot if specified
        if (this[add.Slot] != null) {
            this[add.Slot] = add;
            return new []{(add, add.Amount)};
        }

        var result = new List<(Item, int)>();
        if (stack) {
            mutex.EnterReadLock();
            try {
                foreach (Item item in GetInternalEnumerator()) {
                    if (!CanStack(item, add)) {
                        continue;
                    }

                    int available = item.Metadata.Property.SlotMax - item.Amount;
                    int added = Math.Min(available, add.Amount);
                    add.Amount -= added;
                    item.Amount += added;
                    result.Add((item, added));

                    if (add.Amount <= 0) {
                        return result;
                    }
                }
            } finally {
                mutex.ExitReadLock();
            }
        }

        // All stacks are maxed out at this point, remaining items go in first open slot.
        for (short i = 0; i < Size; i++) {
            if (this[i] != null) {
                continue;
            }

            this[i] = add;
            add.Slot = i;
            result.Add((add, add.Amount));
            return result;
        }

        return Array.Empty<(Item, int)>();
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
                uidToSlot[items[i]!.Uid] = i;
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
            Array.Resize(ref items, newSize);
            return true;
        } finally {
            mutex.ExitWriteLock();
        }
    }

    public bool CanHold(Item item, bool stack = false, int amount = -1) {
        if (OpenSlots > 0) {
            return true;
        }

        if (!stack) {
            return false;
        }

        mutex.EnterReadLock();
        try {
            int remaining = amount < 0 ? item.Amount : amount;
            foreach (Item existing in GetInternalEnumerator()) {
                if (!CanStack(existing, item)) continue;

                int available = existing.Metadata.Property.SlotMax - existing.Amount;
                remaining -= available;
                if (remaining <= 0) {
                    return true;
                }
            }
        } finally {
            mutex.ExitReadLock();
        }

        return false;
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

    private IEnumerable<Item> GetInternalEnumerator() {
        foreach (Item? item in items) {
            if (item == null) continue;
            yield return item;
        }
    }

    private static bool CanStack(Item item, Item stack) {
        return item.Id == stack.Id
               && item.Rarity == stack.Rarity
               && item.Amount < item.Metadata.Property.SlotMax
               && Equals(item.Transfer, stack.Transfer);
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
