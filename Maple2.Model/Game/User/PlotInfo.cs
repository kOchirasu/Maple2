using System;
using System.Collections.Generic;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.Model.Game;

public class PlotInfo {
    public readonly UgcMapGroup Metadata;

    public long Id { get; set; }
    public long OwnerId { get; set; }
    private string name;
    public string Name {
        get => name;
        set {
            if (!string.IsNullOrWhiteSpace(value)) {
                name = value;
            }
        }
    }
    public int MapId { get; set; }
    public int Number { get; init; }
    public int ApartmentNumber { get; init; }

    public long ExpiryTime { get; set; }
    public long LastModified { get; init; }

    public PlotInfo(UgcMapGroup metadata) {
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

public class Plot : PlotInfo {
    public IDictionary<Vector3B, (UgcItemCube Cube, float Rotation)> Cubes = new Dictionary<Vector3B, (UgcItemCube Cube, float Rotation)>();

    public Plot(UgcMapGroup metadata) : base(metadata) { }
}
