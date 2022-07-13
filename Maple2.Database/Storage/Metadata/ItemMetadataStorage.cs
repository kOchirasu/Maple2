using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class ItemMetadataStorage : MetadataStorage<int, ItemMetadata>, ISearchable<ItemMetadata> {
    private const int CACHE_SIZE = 40000; // ~34k total items

    public ItemMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

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

    public List<ItemMetadata> Search(string name) {
        lock (Context) {
            return Context.ItemMetadata
                .Where(item => EF.Functions.Like(item.Name!, $"%{name}%"))
                .ToList();
        }
    }
}
