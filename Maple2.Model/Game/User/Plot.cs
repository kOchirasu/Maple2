using System;
using System.Collections.Generic;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.Model.Game;

public class Plot {
    public readonly UgcMapGroup Metadata;

    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; }
    public int MapId { get; set; }
    public int Number { get; init; }
    public int ApartmentNumber { get; init; }

    public long ExpiryTime { get; set; }
    public long LastModified { get; init; }

    public Vector3B Origin { get; init; }
    public Vector3B Dimensions { get; init; }
    public IDictionary<Vector3B, (UgcItemCube Cube, float Rotation)> Cubes = new Dictionary<Vector3B, (UgcItemCube Cube, float Rotation)>();

    public Plot(UgcMapGroup metadata) {
        Metadata = metadata;
    }

    public PlotState State {
        get {
            if (ExpiryTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                return OwnerId != 0 ? PlotState.Taken : PlotState.Pending;
            }
            if (ExpiryTime == 0) {
                return PlotState.Open;
            }

            return PlotState.Open; // return state=2?
        }
    }
}
