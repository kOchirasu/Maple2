using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Context;
using Maple2.Model.Common;
using Maple2.Model.Metadata;

namespace Maple2.Database.Storage;

public class MapEntityStorage : MetadataStorage<string, MapEntityMetadata> {
    private const int CACHE_SIZE = 1500; // ~1.1k total Maps

    public MapEntityStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

    public MapEntityMetadata? Get(string xblock) {
        if (Cache.TryGet(xblock, out MapEntityMetadata mapEntity)) {
            return mapEntity;
        }

        var breakables = new Dictionary<Guid, Breakable>();
        var liftables = new Dictionary<Vector3B, Liftable>();
        var portals = new Dictionary<int, Portal>();
        var playerSpawns = new Dictionary<int, SpawnPointPC>();
        var npcSpawns = new List<SpawnPointNPC>();
        var regionSpawns = new Dictionary<int, RegionSpawn>();
        var eventNpcSpawns = new Dictionary<int, EventSpawnPointNPC>();
        TaxiStation? taxi = null;
        var breakableActors = new Dictionary<Guid, BreakableActor>();
        var interactActors = new Dictionary<int, InteractActor>();
        lock (Context) {
            foreach (MapEntity entity in Context.MapEntity.Where(entity => entity.XBlock == xblock)) {
                switch (entity.Block.Class) {
                    case MapBlock.Discriminator.Breakable:
                        if (entity.Block is Breakable breakable) {
                            breakables[entity.Guid] = breakable;
                        }
                        break;
                    case MapBlock.Discriminator.BreakableActor:
                        if (entity.Block is BreakableActor breakableActor) {
                            breakableActors[entity.Guid] = breakableActor;
                        }
                        break;
                    case MapBlock.Discriminator.InteractActor:
                        if (entity.Block is InteractActor interactActor) {
                            interactActors[interactActor.InteractId] = interactActor;
                        }
                        break;
                    case MapBlock.Discriminator.Liftable:
                        if (entity.Block is Liftable liftable) {
                            liftables[liftable.Position] = liftable;
                        }
                        break;
                    case MapBlock.Discriminator.Portal:
                        if (entity.Block is Portal portal) {
                            portals[portal.Id] = portal;
                        }
                        break;
                    case MapBlock.Discriminator.RegionSpawn:
                        if (entity.Block is RegionSpawn regionSpawn) {
                            regionSpawns[regionSpawn.Id] = regionSpawn;
                        }
                        break;
                    case MapBlock.Discriminator.SpawnPointPC:
                        if (entity.Block is SpawnPointPC playerSpawn) {
                            playerSpawns[playerSpawn.Id] = playerSpawn;
                        }
                        break;
                    case MapBlock.Discriminator.SpawnPointNPC:
                        if (entity.Block is SpawnPointNPC npcSpawn) {
                            npcSpawns.Add(npcSpawn);
                        }
                        break;
                    case MapBlock.Discriminator.EventSpawnPointNPC:
                        if (entity.Block is EventSpawnPointNPC eventNpcSpawn) {
                            eventNpcSpawns.Add(eventNpcSpawn.Id, eventNpcSpawn);
                        }
                        break;
                    case MapBlock.Discriminator.TaxiStation:
                        if (entity.Block is TaxiStation taxiStation) {
                            taxi = taxiStation;
                        }
                        break;
                }
            }
        }

        mapEntity = new MapEntityMetadata {
            Breakables = breakables,
            Liftables = liftables,
            Portals = portals,
            PlayerSpawns = playerSpawns,
            NpcSpawns = npcSpawns,
            EventNpcSpawns = eventNpcSpawns,
            RegionSpawns = regionSpawns,
            Taxi = taxi,
            BreakableActors = breakableActors,
            InteractActors = interactActors,
        };
        Cache.AddReplace(xblock, mapEntity);

        return mapEntity;
    }

    public bool Contains(string xblock) {
        return Get(xblock) != null;
    }
}
