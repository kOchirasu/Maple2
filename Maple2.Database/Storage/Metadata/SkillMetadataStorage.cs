using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Caching;
using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class SkillMetadataStorage : MetadataStorage<int, SkillMetadata>, ISearchable<SkillMetadata> {
    private const int CACHE_SIZE = 10000; // ~10k total items
    private const int EFFECT_CACHE_SIZE = 15000;  // ~14.5k total additional effect levels

    protected readonly LRUCache<(int Id, short Level), AdditionalEffectMetadata> EffectCache;

    public SkillMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) {
        EffectCache = new LRUCache<(int, short), AdditionalEffectMetadata>(EFFECT_CACHE_SIZE, (int)(EFFECT_CACHE_SIZE * 0.05));
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

    public bool TryGetEffect(int id, short level, [NotNullWhen(true)] out AdditionalEffectMetadata? effect) {
        if (EffectCache.TryGet((id, level), out effect)) {
            return true;
        }

        lock (Context) {
            effect = Context.AdditionalEffectMetadata.Find(new {id, level});
        }

        if (effect == null) {
            return false;
        }

        EffectCache.AddReplace((id, level), effect);
        return true;
    }

    public List<SkillMetadata> Search(string name) {
        lock (Context) {
            return Context.SkillMetadata
                .Where(skill => EF.Functions.Like(skill.Name!, $"%{name}%"))
                .ToList();
        }
    }
}
