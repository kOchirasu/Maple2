using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Tools;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class TrophyMetadataStorage : MetadataStorage<int, TrophyMetadata> {
    private const int CACHE_SIZE = 2500; // ~2.2k total trophies

    public TrophyMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

    public bool TryGet(int id, [NotNullWhen(true)] out TrophyMetadata? trophy) {
        if (Cache.TryGet(id, out trophy)) {
            return true;
        }

        lock (Context) {
            // Double-checked locking
            if (Cache.TryGet(id, out trophy)) {
                return true;
            }

            trophy = Context.TrophyMetadata.Find(id);

            if (trophy == null) {
                return false;
            }

            Cache.AddReplace(id, trophy);
        }

        return true;
    }

    public IEnumerable<TrophyMetadata> GetMany(TrophyConditionType type) {
        return Context.TrophyMetadata.Where(trophy => trophy.ConditionType == type);
    }
}
