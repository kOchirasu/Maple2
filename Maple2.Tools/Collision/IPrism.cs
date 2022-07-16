using System.Numerics;

namespace Maple2.Tools.Collision;

public interface IPrism {
    public float MinHeight { get; }
    public float MaxHeight { get; }

    public bool Contains(in Vector3 point);
}

public class Prism : IPrism {
    public readonly IPolygon Polygon;
    public float MinHeight { get; }
    public float MaxHeight { get; }

    public Prism(IPolygon shape, float baseHeight, float height) {
        Polygon = shape;
        MinHeight = baseHeight;
        MaxHeight = baseHeight + height;
    }

    public bool Contains(in Vector3 point) {
        return MinHeight <= point.Z && point.Z <= MaxHeight && Polygon.Contains(point.X, point.Y);
    }

    public override string ToString() {
        return $"{Polygon}, [{MinHeight}, {MaxHeight}]";
    }
}

public class CompositePrism : IPrism {
    public readonly IPolygon[] Polygons;
    public float MinHeight { get; }
    public float MaxHeight { get; }

    public CompositePrism(IPolygon[] shapes, float baseHeight, float height) {
        Polygons = shapes;
        MinHeight = baseHeight;
        MaxHeight = baseHeight + height;
    }

    public bool Contains(in Vector3 point) {
        if (point.Z < MinHeight || point.Z > MaxHeight) {
            return false;
        }

        foreach (IPolygon polygon in Polygons) {
            if (polygon.Contains(point.X, point.Y)) {
                return true;
            }
        }
        return false;
    }
}
