using System.Numerics;

namespace Maple2.Tools.Collision;

public sealed class HoleCircle : Circle {
    private readonly Circle hole;

    public HoleCircle(in Vector2 origin, float innerRadius, float outerRadius) : base(origin, outerRadius) {
        hole = new Circle(origin, innerRadius);
    }

    public override bool Contains(in Vector2 point) {
        return base.Contains(point) && !hole.Contains(point);
    }

    // Inherited GetAxes() and AxisProjection() includes hole, but is unused.

    public override bool Intersects(IPolygon other) {
        return other switch {
            Circle circle => IntersectsCircle(circle) && !hole.IntersectsCircle(circle),
            Polygon polygon => polygon.Intersects(this) && !polygon.Intersects(hole),
            _ => false,
        };
    }

    public override string ToString() {
        return $"Outer:{base.ToString()}, Inner:{hole}";
    }
}
