using System.Collections.Generic;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.Model.Game;

public class Plot {
    public readonly UgcMapGroup Metadata;

    public long Uid { get; set; }
    public long OwnerId { get; set; }
    public int MapId { get; set; }
    public int Number { get; set; }
    public int ApartmentNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlotState State { get; set; }
    public long ExpiryTime { get; set; }
    public long LastModified { get; set; }

    public IDictionary<Vector3B, (UgcItemCube Cube, float Rotation)> Cubes = new Dictionary<Vector3B, (UgcItemCube Cube, float Rotation)>();

    public Plot(UgcMapGroup metadata) {
        Metadata = metadata;
    }
}
