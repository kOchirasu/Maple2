using Caching;
using Maple2.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public abstract class MetadataStorage<TK, TV> {
    protected readonly MetadataContext Context;
    protected readonly LRUCache<TK, TV> Cache;

    public MetadataStorage(MetadataContext context, int capacity) {
        Context = context;
        Cache = new LRUCache<TK, TV>(capacity, (int)(capacity * 0.05));
    }

    public void InvalidateCache() {
        Cache.Clear();
    }
}
