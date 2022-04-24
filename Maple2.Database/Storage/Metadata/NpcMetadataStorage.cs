using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage; 

public class NpcMetadataStorage : MetadataStorage<int, NpcMetadata> {
    private const int CACHE_SIZE = 7500; // ~7.4k total npcs
    
    public NpcMetadataStorage(DbContextOptions options) : base(options, CACHE_SIZE) { }
    
    public NpcMetadata Get(int id) {
        if (Cache.TryGet(id, out NpcMetadata npc)) {
            return npc;
        }

        using var context = new MetadataContext(Options);
        npc = context.NpcMetadata.Find(id);
        Cache.AddReplace(id, npc);

        return npc;
    }
    
    public bool Contains(int id) {
        return Get(id) != null;
    }
}
