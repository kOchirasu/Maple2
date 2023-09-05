using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Model.Enum;
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
            // Double-checked locking
            if (Cache.TryGet(id, out quest)) {
                return true;
            }

            quest = Context.QuestMetadata.Find(id);

            if (quest == null) {
                return false;
            }

            Cache.AddReplace(id, quest);
        }

        return true;
    }

    public IEnumerable<QuestMetadata> GetQuests() {
        lock (Context) {
            return Context.QuestMetadata
                .ToList();
        }
    }

    public IList<QuestMetadata> GetQuestsByType(QuestType type) {
        lock (Context) {
            return Context.QuestMetadata
                .Where(quest => quest.Basic.Type == type)
                .ToList();
        }
    }

    public ICollection<QuestMetadata> GetQuestsByNpc(int npcId) {
        lock (Context) {
            // this works but seems questionable?? Cannot search before AsEnumerable
            return Context.QuestMetadata
                .AsEnumerable()
                .Where(quest => quest.Basic.StartNpc == npcId)
                .ToList();
        }
    }

    public List<QuestMetadata> Search(string name) {
        lock (Context) {
            return Context.QuestMetadata
                .Where(quest => EF.Functions.Like(quest.Name!, $"%{name}%"))
                .ToList();
        }
    }
}
