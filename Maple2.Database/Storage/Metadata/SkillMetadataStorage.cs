using System.Diagnostics.CodeAnalysis;
using Caching;
using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class SkillMetadataStorage : MetadataStorage<int, SkillMetadata> {
    private const int CACHE_SIZE = 10000; // ~10k total items
    private const int MAGIC_PATH_CACHE_SIZE = 3000;

    protected readonly LRUCache<long, MagicPathMetadata> MagicPathCache;

    public SkillMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) {
        MagicPathCache =
            new LRUCache<long, MagicPathMetadata>(MAGIC_PATH_CACHE_SIZE, (int)(MAGIC_PATH_CACHE_SIZE * 0.05));
    }

    public bool TryGet(int id, [NotNullWhen(true)] out SkillMetadata? skill) {
        if (Cache.TryGet(id, out skill)) {
            return true;
        }

        lock (Context) {
            skill = Context.SkillMetadata.Find(id);
        }

        if (skill == null) {
            return false;
        }

        Cache.AddReplace(id, skill);
        return true;
    }

    public bool TryGetMagicPath(long id, [NotNullWhen(true)] out MagicPathMetadata? magicPath) {
        if (MagicPathCache.TryGet(id, out magicPath)) {
            return true;
        }

        lock (Context) {
            magicPath = Context.MagicPathMetadata.Find(id);
        }

        if (magicPath == null) {
            return false;
        }

        MagicPathCache.AddReplace(id, magicPath);
        return true;
    }
}
