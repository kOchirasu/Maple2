using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class QuestMetadataStorage : MetadataStorage<int, QuestMetadata>, ISearchable<QuestMetadata> {
    private const int CACHE_SIZE = 2500; // ~2.2k total items

    public QuestMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

    public bool TryGet(int id, [NotNullWhen(true)] out QuestMetadata? quest) {
        if (Cache.TryGet(id, out quest)) {
            return true;
        }

        lock (Context) {
            quest = Context.QuestMetadata.Find(id);
        }

        if (quest == null) {
            return false;
        }

        Cache.AddReplace(id, quest);
        return true;
    }

    public List<QuestMetadata> Search(string name) {
        lock (Context) {
            return Context.QuestMetadata
                .Where(quest => EF.Functions.Like(quest.Name!, $"%{name}%"))
                .ToList();
        }
    }
}
