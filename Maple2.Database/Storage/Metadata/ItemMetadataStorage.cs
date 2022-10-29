using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class ItemMetadataStorage : MetadataStorage<int, ItemMetadata>, ISearchable<ItemMetadata> {
    private const int CACHE_SIZE = 40000; // ~34k total items

    private readonly ConcurrentDictionary<int, ItemMetadata> petToItem = new();

    public ItemMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) {
        IndexPets();
    }

    public bool TryGet(int id, [NotNullWhen(true)] out ItemMetadata? item) {
        if (Cache.TryGet(id, out item)) {
            return true;
        }

        lock (Context) {
            // Double-checked locking
            if (Cache.TryGet(id, out item)) {
                return true;
            }

            item = Context.ItemMetadata.Find(id);

            if (item == null) {
                return false;
            }

            Cache.AddReplace(id, item);
        }

        return true;
    }

    public bool TryGetPet(int petId, [NotNullWhen(true)] out ItemMetadata? item) {
        return petToItem.TryGetValue(petId, out item);
    }

    public override void InvalidateCache() {
        base.InvalidateCache();
        IndexPets();
    }

    public List<ItemMetadata> Search(string name) {
        lock (Context) {
            return Context.ItemMetadata
                .Where(item => EF.Functions.Like(item.Name!, $"%{name}%"))
                .ToList();
        }
    }

    private readonly FormattableString petQuery = $"SELECT * FROM `maple-data`.item WHERE JSON_EXTRACT(Property, '$.PetId') > 0";
    private void IndexPets() {
        petToItem.Clear();

        lock (Context) {
            foreach (ItemMetadata item in Context.ItemMetadata.FromSqlInterpolated(petQuery)) {
                Cache.AddReplace(item.Id, item);
                petToItem[item.Property.PetId] = item;
            }
        }
    }
}
