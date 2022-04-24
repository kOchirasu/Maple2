using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class ItemMetadataStorage : MetadataStorage<int, ItemMetadata> {
    private const int CACHE_SIZE = 40000; // ~34k total items

    public ItemMetadataStorage(DbContextOptions options) : base(options, CACHE_SIZE) { }

    public ItemMetadata Get(int id) {
        if (Cache.TryGet(id, out ItemMetadata item)) {
            return item;
        }

        using var context = new MetadataContext(Options);
        item = context.ItemMetadata.Find(id);
        Cache.AddReplace(id, item);

        return item;
    }

    public bool Contains(int id) {
        return Get(id) != null;
    }
}
