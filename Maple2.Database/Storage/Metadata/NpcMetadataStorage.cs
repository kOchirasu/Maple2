using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Caching;
using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class NpcMetadataStorage : MetadataStorage<int, NpcMetadata>, ISearchable<NpcMetadata> {
    private const int CACHE_SIZE = 7500; // ~7.4k total npcs
    private const int ANI_CACHE_SIZE = 2500;

    protected readonly LRUCache<string, AnimationMetadata> AniCache;

    public NpcMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) {
        AniCache = new LRUCache<string, AnimationMetadata>(ANI_CACHE_SIZE, (int)(ANI_CACHE_SIZE * 0.05));
    }

    public bool TryGet(int id, [NotNullWhen(true)] out NpcMetadata? npc) {
        if (Cache.TryGet(id, out npc)) {
            return true;
        }

        lock (Context) {
            npc = Context.NpcMetadata.Find(id);
        }

        if (npc == null) {
            return false;
        }

        Cache.AddReplace(id, npc);
        return true;
    }

    public List<NpcMetadata> Search(string name) {
        lock (Context) {
            return Context.NpcMetadata
                .Where(npc => EF.Functions.Like(npc.Name!, $"%{name}%"))
                .ToList();
        }
    }

    public AnimationMetadata? GetAnimation(string model) {
        if (AniCache.TryGet(model, out AnimationMetadata? animation)) {
            return animation;
        }

        lock (Context) {
            animation = Context.AnimationMetadata.Find(model);
        }

        if (animation == null) {
            return null;
        }

        AniCache.AddReplace(model, animation);

        return animation;
    }
}
