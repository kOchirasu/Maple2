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

    public SkillMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

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

    public List<SkillMetadata> Search(string name) {
        lock (Context) {
            return Context.SkillMetadata
                .Where(skill => EF.Functions.Like(skill.Name!, $"%{name}%"))
                .ToList();
        }
    }
}
