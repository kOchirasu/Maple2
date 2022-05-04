using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public class MapMetadataStorage : MetadataStorage<int, MapMetadata> {
    private const int CACHE_SIZE = 1500; // ~1.1k total Maps

    public MapMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

    public bool TryGet(int id, out MapMetadata map) {
        if (Cache.TryGet(id, out map)) {
            return true;
        }

        lock (Context) {
            map = Context.MapMetadata.Find(id);
        }

        if (map == null) {
            return false;
        }

        Cache.AddReplace(id, map);
        return true;
    }
}
