﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Maple2.Database.Context;
using Maple2.Model.Common;
using Maple2.Model.Metadata;
using Maple2.Tools.Collision;

namespace Maple2.Database.Storage;

public class MapEntityStorage : MetadataStorage<string, MapEntityMetadata> {
    private const int CACHE_SIZE = 1500; // ~1.1k total Maps

    private const float MAP_LIMIT = sbyte.MaxValue * 150f;
    private static readonly Prism LargeBoundingBox = new(new BoundingBox(
        new Vector2(-MAP_LIMIT, -MAP_LIMIT),
        new Vector2(MAP_LIMIT, MAP_LIMIT)),
        -MAP_LIMIT, MAP_LIMIT * 2);

    public MapEntityStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

    public MapEntityMetadata? Get(string xblock) {
        if (Cache.TryGet(xblock, out MapEntityMetadata mapEntity)) {
            return mapEntity;
        }

        lock (Context) {
            // Double-checked locking
            if (Cache.TryGet(xblock, out mapEntity)) {
                return mapEntity;
            }

            var breakables = new Dictionary<Guid, Breakable>();
            var liftables = new Dictionary<Guid, Liftable>();
            var objectWeapons = new Dictionary<Vector3B, ObjectWeapon>();
            var portals = new Dictionary<int, Portal>();
            var playerSpawns = new Dictionary<int, SpawnPointPC>();
            var npcSpawns = new List<SpawnPointNPC>();
            var regionSpawns = new Dictionary<int, Ms2RegionSpawn>();
            var regionSkills = new List<Ms2RegionSkill>();
            var eventNpcSpawns = new Dictionary<int, EventSpawnPointNPC>();
            TaxiStation? taxi = null;
            Prism? bounding = null;
            var breakableActors = new Dictionary<Guid, BreakableActor>();
            var interacts = new Dictionary<Guid, InteractObject>();
            var triggerModels = new Dictionary<int, TriggerModel>();
            var triggers = new List<Trigger>();
            foreach (MapEntity entity in Context.MapEntity.Where(entity => entity.XBlock == xblock)) {
                switch (entity.Block) {
                    case Breakable breakable:
                        breakables[entity.Guid] = breakable;
                        break;
                    case BreakableActor breakableActor:
                        breakableActors[entity.Guid] = breakableActor;
                        break;
                    case Liftable liftable:
                        liftables[entity.Guid] = liftable;
                        break;
                    case ObjectWeapon objectWeapon:
                        objectWeapons[objectWeapon.Position] = objectWeapon;
                        break;
                    case Portal portal:
                        portals[portal.Id] = portal;
                        break;
                    case Ms2RegionSpawn regionSpawn:
                        regionSpawns[regionSpawn.Id] = regionSpawn;
                        break;
                    case Ms2RegionSkill regionSkill:
                        regionSkills.Add(regionSkill);
                        break;
                    case SpawnPointPC playerSpawn:
                        playerSpawns[playerSpawn.Id] = playerSpawn;
                        break;
                    case SpawnPointNPC npcSpawn:
                        if (npcSpawn is EventSpawnPointNPC eventNpcSpawn) {
                            eventNpcSpawns.Add(eventNpcSpawn.Id, eventNpcSpawn);
                        } else {
                            npcSpawns.Add(npcSpawn);
                        }
                        break;
                    case TaxiStation taxiStation:
                        Debug.Assert(taxi == null, $"Multiple taxi stations found in xblock:{xblock}");
                        taxi = taxiStation;
                        break;
                    case TriggerModel triggerModel:
                        triggerModels.Add(triggerModel.Id, triggerModel);
                        break;
                    case Ms2InteractActor or Ms2InteractDisplay or Ms2InteractMesh or Ms2SimpleUiObject or Ms2Telescope:
                        interacts.Add(entity.Guid, (InteractObject) entity.Block);
                        break;
                    case Ms2TriggerActor or Ms2TriggerAgent or Ms2TriggerBox or Ms2TriggerCamera or Ms2TriggerCube or Ms2TriggerEffect or
                        Ms2TriggerLadder or Ms2TriggerMesh or Ms2TriggerRope or Ms2TriggerSkill or Ms2TriggerSound:
                        triggers.Add((Trigger) entity.Block);
                        break;
                    case Ms2Bounding mapBounding:
                        var box = new BoundingBox(
                            new Vector2(mapBounding.Position1.X, mapBounding.Position1.Y),
                            new Vector2(mapBounding.Position2.X, mapBounding.Position2.Y)
                        );
                        float baseHeight = Math.Min(mapBounding.Position1.Z, mapBounding.Position2.Z);
                        float height = Math.Abs(mapBounding.Position2.Z - mapBounding.Position1.Z);
                        bounding = new Prism(box, baseHeight, height);
                        break;
                }
            }

            mapEntity = new MapEntityMetadata {
                Breakables = breakables,
                Liftables = liftables,
                ObjectWeapons = objectWeapons,
                Portals = portals,
                PlayerSpawns = playerSpawns,
                NpcSpawns = npcSpawns,
                EventNpcSpawns = eventNpcSpawns,
                RegionSpawns = regionSpawns,
                RegionSkills = regionSkills,
                Taxi = taxi,
                BoundingBox = bounding ?? LargeBoundingBox,
                BreakableActors = breakableActors,
                Interacts = interacts,
                TriggerModels = triggerModels,
                Trigger = new TriggerStorage(triggers),
            };
            Cache.AddReplace(xblock, mapEntity);
        }

        return mapEntity;
    }

    public bool Contains(string xblock) {
        return Get(xblock) != null;
    }
}
