using System.Numerics;

namespace Maple2.Tools.Collision;

public class Prism<T> where T : IPolygon {
    public readonly IPolygon Polygon;
    public readonly float MinHeight;
    public readonly float MaxHeight;

    public Prism(IPolygon shape, float baseHeight, float height) {
        Polygon = shape;
        MinHeight = baseHeight;
        MaxHeight = baseHeight + height;
    }

    public bool Contains(Vector3 point) {
        return MinHeight <= point.Z && point.Z <= MaxHeight && Polygon.Contains(point.X, point.Y);
    }
}
