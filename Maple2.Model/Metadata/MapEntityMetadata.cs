using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Maple2.Model.Common;
using Maple2.Tools.Collision;

namespace Maple2.Model.Metadata;

public class MapEntityMetadata {
    public IReadOnlyDictionary<Guid, Breakable> Breakables { get; init; }
    public IReadOnlyDictionary<Guid, Liftable> Liftables { get; init; }
    public IReadOnlyDictionary<Vector3B, ObjectWeapon> ObjectWeapons { get; init; }
    public IReadOnlyDictionary<int, Portal> Portals { get; init; }
    public IReadOnlyDictionary<int, SpawnPointPC> PlayerSpawns { get; init; }
    public IReadOnlyList<SpawnPointNPC> NpcSpawns { get; init; }
    public IReadOnlyDictionary<int, Ms2RegionSpawn> RegionSpawns { get; init; }
    public IReadOnlyList<Ms2RegionSkill> RegionSkills { get; init; }
    public IReadOnlyDictionary<int, EventSpawnPointNPC> EventNpcSpawns { get; init; }
    public TaxiStation? Taxi { get; init; }
    public Telescope? Telescope { get; init; }
    public Prism BoundingBox { get; init; }

    public IReadOnlyDictionary<Guid, BreakableActor> BreakableActors { get; init; }
    public IReadOnlyDictionary<int, InteractActor> InteractActors { get; init; }

    public IReadOnlyDictionary<int, TriggerModel> TriggerModels { get; init; }
    public ITriggerStorage Trigger { get; init; }
}

public interface ITriggerStorage : IReadOnlyDictionary<int, Trigger> {
    public ImmutableArray<Ms2TriggerActor> Actors { get; }
    public ImmutableArray<Ms2TriggerBox> Boxes { get; }
    public ImmutableArray<Ms2TriggerCamera> Cameras { get; }
    public ImmutableArray<Ms2TriggerCube> Cubes { get; }
    public ImmutableArray<Ms2TriggerEffect> Effects { get; }
    public ImmutableArray<Ms2TriggerLadder> Ladders { get; }
    public ImmutableArray<Ms2TriggerMesh> Meshes { get; }
    public ImmutableArray<Ms2TriggerRope> Ropes { get; }
    public ImmutableArray<Ms2TriggerSkill> Skills { get; }
    public ImmutableArray<Ms2TriggerSound> Sounds { get; }

    public bool TryGet<T>(int key, [NotNullWhen(true)] out T? trigger) where T : Trigger;
}
