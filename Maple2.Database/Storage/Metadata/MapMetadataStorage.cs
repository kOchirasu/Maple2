using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage; 

public class MapMetadataStorage : MetadataStorage<int, MapMetadata> {
    private const int CACHE_SIZE = 1500; // ~1.1k total Maps
    
    public MapMetadataStorage(DbContextOptions options) : base(options, CACHE_SIZE) { }
    
    public MapMetadata Get(int id) {
        if (Cache.TryGet(id, out MapMetadata Map)) {
            return Map;
        }

        using var context = new MetadataContext(Options);
        Map = context.MapMetadata.Find(id);
        Cache.AddReplace(id, Map);

        return Map;
    }
    
    public bool Contains(int id) {
        return Get(id) != null;
    }
}
