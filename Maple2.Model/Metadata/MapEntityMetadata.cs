using System;
using System.Collections.Generic;
using Maple2.Model.Common;

namespace Maple2.Model.Metadata;

public class MapEntityMetadata {
    public IReadOnlyDictionary<Guid, Breakable> Breakables { get; init; }
    public IReadOnlyDictionary<Guid, Liftable> Liftables { get; init; }
    public IReadOnlyDictionary<Vector3B, ObjectWeapon> ObjectWeapons { get; init; }
    public IReadOnlyDictionary<int, Portal> Portals { get; init; }
    public IReadOnlyDictionary<int, SpawnPointPC> PlayerSpawns { get; init; }
    public IReadOnlyList<SpawnPointNPC> NpcSpawns { get; init; }
    public IReadOnlyDictionary<int, RegionSpawn> RegionSpawns { get; init; }
    public IReadOnlyDictionary<int, EventSpawnPointNPC> EventNpcSpawns { get; init; }
    public TaxiStation? Taxi { get; init; }
    public Telescope? Telescope { get; init; }

    public IReadOnlyDictionary<Guid, BreakableActor> BreakableActors { get; init; }
    public IReadOnlyDictionary<int, InteractActor> InteractActors { get; init; }
}
