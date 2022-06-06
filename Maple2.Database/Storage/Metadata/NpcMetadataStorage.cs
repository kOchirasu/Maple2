using Caching;
using Maple2.Database.Context;
using Maple2.Model.Metadata;

namespace Maple2.Database.Storage;

public class NpcMetadataStorage : MetadataStorage<int, NpcMetadata> {
    private const int CACHE_SIZE = 7500; // ~7.4k total npcs
    private const int ANI_CACHE_SIZE = 2500;

    protected readonly LRUCache<string, AnimationMetadata> AniCache;

    public NpcMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) {
        AniCache = new LRUCache<string, AnimationMetadata>(ANI_CACHE_SIZE, (int)(ANI_CACHE_SIZE * 0.05));
    }

    public NpcMetadata Get(int id) {
        if (Cache.TryGet(id, out NpcMetadata npc)) {
            return npc;
        }

        lock (Context) {
            npc = Context.NpcMetadata.Find(id);
        }

        Cache.AddReplace(id, npc);

        return npc;
    }

    public AnimationMetadata GetAnimation(string model) {
        if (AniCache.TryGet(model, out AnimationMetadata animation)) {
            return animation;
        }

        lock (Context) {
            animation = Context.AnimationMetadata.Find(model);
        }

        AniCache.AddReplace(model, animation);

        return animation;
    }

    public bool Contains(int id) {
        return Get(id) != null;
    }
}
