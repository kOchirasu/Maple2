using System.Collections.Generic;
using Maple2.Model.Common;

namespace Maple2.Model.Metadata;

public class MapEntityMetadata {
    public IReadOnlyDictionary<int, Breakable> Breakables { get; init; }
    public IReadOnlyDictionary<int, InteractActor> InteractActors { get; init; }
    public IReadOnlyDictionary<Vector3B, Liftable> Liftables { get; init; }
    public IReadOnlyDictionary<int, Portal> Portals { get; init; }
    public IReadOnlyDictionary<int, SpawnPointPC> PlayerSpawns { get; init; }
    public TaxiStation? Taxi { get; init; }
}
