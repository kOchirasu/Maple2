using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Model.Game;
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
    private readonly WeightedSet<ItemMetadata> pets;
    private readonly List<int> spawnedMobs;
    private readonly List<int> spawnedPets;
    private int spawnTick;

    public FieldMobSpawn(FieldManager field, int objectId, MapMetadataSpawn metadata, WeightedSet<NpcMetadata> npcs, WeightedSet<ItemMetadata> pets) : base(field, objectId, metadata) {
        this.npcs = npcs;
        this.pets = pets;
        spawnedMobs = new List<int>(metadata.Population);
        spawnedPets = new List<int>(metadata.PetPopulation);
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

        // No respawn without a non-zero cooldown.
        if (Value.Cooldown <= 0) {
            return;
        }

#if DEBUG
        // 2x faster respawns.
        const int spawnRate = 500;
#else
        const int spawnRate = 1000;
#endif

        if (spawnedMobs.Count == 0) {
            spawnTick = Math.Min(spawnTick, Environment.TickCount + Value.Cooldown * spawnRate);
        } else if (spawnTick == int.MaxValue) {
            spawnTick = Environment.TickCount + Value.Cooldown * spawnRate * FORCE_SPAWN_MULTIPLIER;
        }
    }

    public override void Sync() {
        if (Environment.TickCount < spawnTick) {
            return;
        }

        spawnTick = int.MaxValue;
        for (int i = spawnedMobs.Count; i < Value.Population; i++) {
            FieldNpc fieldNpc = Field.SpawnNpc(npcs.Get(), GetRandomSpawn(), Rotation, owner: this);
            spawnedMobs.Add(fieldNpc.ObjectId);

            Field.Broadcast(FieldPacket.AddNpc(fieldNpc));
            Field.Broadcast(ProxyObjectPacket.AddNpc(fieldNpc));
        }

        if (Value.PetSpawnRate <= 0 || pets.Count <= 0 || spawnedPets.Count >= Value.PetPopulation) {
            return;
        }

        if (Random.Shared.Next(PET_SPAWN_RATE_TOTAL) < Value.PetSpawnRate) {
            var pet = new Item(pets.Get());
            FieldPet? fieldPet = Field.SpawnPet(pet, GetRandomSpawn(), Rotation, owner: this);
            if (fieldPet == null) {
                return;
            }

            spawnedPets.Add(fieldPet.ObjectId);

            Field.Broadcast(FieldPacket.AddPet(fieldPet));
            Field.Broadcast(ProxyObjectPacket.AddPet(fieldPet));
        }
    }

    private Vector3 GetRandomSpawn() {
        int spawnX = Random.Shared.Next((int) Position.X - PET_SPAWN_RADIUS, (int) Position.X + PET_SPAWN_RADIUS);
        int spawnY = Random.Shared.Next((int) Position.Y - PET_SPAWN_RADIUS, (int) Position.Y + PET_SPAWN_RADIUS);
        return new Vector3(spawnX, spawnY, Position.Z);
    }
}
