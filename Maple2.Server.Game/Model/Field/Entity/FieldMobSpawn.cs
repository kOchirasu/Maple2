using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;
using Maple2.Tools;
using Serilog;

namespace Maple2.Server.Game.Model;

/// <summary>
/// SpawnPoint on field with random NPCs.
/// </summary>
public class FieldMobSpawn : FieldEntity<MapMetadataSpawn> {
    private const int FORCE_SPAWN_MULTIPLIER = 2;
    private const int PET_SPAWN_RADIUS = 150;
    private const int PET_SPAWN_RATE_TOTAL = 10000;

    private readonly WeightedSet<NpcMetadata> npcs;
    private readonly List<int> spawnedMobs;
    private readonly List<int> spawnedPets;
    private int forceSpawnTick;

    public FieldMobSpawn(FieldManager field, int objectId, MapMetadataSpawn metadata, WeightedSet<NpcMetadata> npcs) : base(field, objectId, metadata) {
        this.npcs = npcs;
        this.spawnedMobs = new List<int>(metadata.Population);
        this.spawnedPets = new List<int>(metadata.PetPopulation);

        if (Value.Cooldown <= 0) {
            Log.Logger.Information("No respawn for mapId:{MapId} spawnId:{SpawnId}", Field.MapId, Value.Id);
        }
    }

    public void Despawn(int objectId) {
        // Pets do not impact spawn cycle.
        spawnedPets.Remove(objectId);

        if (!spawnedMobs.Remove(objectId)) {
            return;
        }

        if (Value.Cooldown > 0 && forceSpawnTick == int.MaxValue) {
            forceSpawnTick = Environment.TickCount + Value.Cooldown * FORCE_SPAWN_MULTIPLIER;
        }
    }

    public override void Sync() {
        if (spawnedMobs.Count > 0 && Environment.TickCount < forceSpawnTick) {
            return;
        }

        forceSpawnTick = int.MaxValue;
        for (int i = spawnedMobs.Count; i < Value.Population; i++) {
            NpcMetadata npc = npcs.Get();
            int spawnX = Random.Shared.Next((int) Position.X - PET_SPAWN_RADIUS, (int) Position.X + PET_SPAWN_RADIUS);
            int spawnY = Random.Shared.Next((int) Position.Y - PET_SPAWN_RADIUS, (int) Position.Y + PET_SPAWN_RADIUS);
            var spawnPosition = new Vector3(spawnX, spawnY, Position.Z);
            FieldNpc fieldNpc = Field.SpawnNpc(npc, spawnPosition, Rotation);
            spawnedMobs.Add(fieldNpc.ObjectId);

            Field.Broadcast(FieldPacket.AddNpc(fieldNpc));
            Field.Broadcast(ProxyObjectPacket.AddNpc(fieldNpc));
        }

        if (Value.PetSpawnRate <= 0 || spawnedPets.Count < Value.PetPopulation) {
            return;
        }

        if (Random.Shared.Next(PET_SPAWN_RATE_TOTAL) < Value.PetSpawnRate) {
            int petId = Value.PetIds[Random.Shared.Next(Value.PetIds.Length)];
            Log.Logger.Information("TODO: Spawning pet:{PetId}", petId);
        }
    }
}
