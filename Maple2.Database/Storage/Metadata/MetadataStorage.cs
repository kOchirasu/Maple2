using Caching;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage.Metadata;

public abstract class MetadataStorage<TK, TV> {
    protected readonly DbContextOptions Options;
    protected readonly LRUCache<TK, TV> Cache;

    public MetadataStorage(DbContextOptions options, int capacity) {
        Options = options;
        Cache = new LRUCache<TK, TV>(capacity, (int)(capacity * 0.05));
    }

    public void InvalidateCache() {
        Cache.Clear();
    }
}
