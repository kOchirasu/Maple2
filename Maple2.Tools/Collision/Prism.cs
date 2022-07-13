using System.Numerics;

namespace Maple2.Tools.Collision;

public class Prism {
    public readonly IPolygon Polygon;
    public readonly float MinHeight;
    public readonly float MaxHeight;

    public Prism(IPolygon shape, float baseHeight, float height) {
        Polygon = shape;
        MinHeight = baseHeight;
        MaxHeight = baseHeight + height;
    }

    public bool Contains(in Vector3 point) {
        return MinHeight <= point.Z && point.Z <= MaxHeight && Polygon.Contains(point.X, point.Y);
    }
}
