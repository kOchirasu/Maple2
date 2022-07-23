using System;
using System.Numerics;

namespace Maple2.Tools.Collision;

public class Circle : IPolygon {
    private readonly Vector2 origin;
    private readonly float radius;

    public Circle(in Vector2 origin, float radius) {
        this.origin = origin;
        this.radius = radius;
    }

    public virtual bool Contains(in Vector2 point) {
        return Vector2.DistanceSquared(origin, point) <= radius * radius;
    }

    public Vector2[] GetAxes(Polygon? other) {
        if (other == null) {
            throw new ArgumentException("Cannot compute Axes for circle without polygon");
        }

        float min = float.MaxValue;
        int result = 0;
        for (int i = 0; i < other.Points.Length; i++) {
            float distanceSquared = Vector2.DistanceSquared(origin, other.Points[i]);
            if (distanceSquared < min) {
                min = distanceSquared;
                result = i;
            }
        }

        return new[] {other.Points[result] - origin};
    }

    public Range AxisProjection(Vector2 axis) {
        float centerProjection = Vector2.Dot(axis, origin);
        return new Range(centerProjection - radius, centerProjection + radius);
    }

    public virtual bool Intersects(IPolygon other) {
        return other switch {
            Circle circle => IntersectsCircle(circle),
            Polygon polygon => polygon.Intersects(this),
            _ => false,
        };
    }

    // Optimization to avoid type-check when we already know the shape is a circle.
    internal bool IntersectsCircle(Circle other) {
        float maxDistance = radius + other.radius;
        return Vector2.DistanceSquared(origin, other.origin) <= maxDistance * maxDistance;
    }

    public override string ToString() {
        return $"Origin:{origin}, Radius:{radius}";
    }
}
