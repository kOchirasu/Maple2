using System.Collections.Generic;

namespace Maple2.Model.Metadata; 

public class MapEntityMetadata {
    public IReadOnlyDictionary<int, Portal> Portals { get; init; }
    public IReadOnlyDictionary<int, SpawnPointPC> PlayerSpawns { get; init; }
}
