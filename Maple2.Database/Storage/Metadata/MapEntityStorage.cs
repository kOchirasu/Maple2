using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage; 

public class MapEntityStorage : MetadataStorage<string, MapEntityMetadata> {
    private const int CACHE_SIZE = 1500; // ~1.1k total Maps
    
    public MapEntityStorage(DbContextOptions options) : base(options, CACHE_SIZE) { }
    
    public MapEntityMetadata Get(string xblock) {
        if (Cache.TryGet(xblock, out MapEntityMetadata mapEntity)) {
            return mapEntity;
        }

        using var context = new MetadataContext(Options);
        var portals = new Dictionary<int, Portal>();
        var playerSpawns = new Dictionary<int, SpawnPointPC>();
        foreach (MapEntity entity in context.MapEntity.Where(entity => entity.XBlock == xblock)) {
            switch (entity.Block.Class) {
                case MapBlock.Discriminator.Portal:
                    if (entity.Block is Portal portal) {
                        portals[portal.Id] = portal;
                    }
                    break;
                case MapBlock.Discriminator.SpawnPointPC:
                    if (entity.Block is SpawnPointPC playerSpawn) {
                        playerSpawns[playerSpawn.Id] = playerSpawn;
                    }
                    break;
            }
        }

        mapEntity = new MapEntityMetadata {
            Portals = portals,
            PlayerSpawns = playerSpawns,
        };
        Cache.AddReplace(xblock, mapEntity);

        return mapEntity;
    }
    
    public bool Contains(string xblock) {
        return Get(xblock) != null;
    }
}
