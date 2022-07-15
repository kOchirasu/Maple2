using System;
using System.Collections.Generic;
using System.Numerics;

namespace Maple2.Tools.Collision;

public class Circle : IPolygon {
    IReadOnlyList<Vector2> IPolygon.Points => Array.Empty<Vector2>();
    private readonly Vector2 origin;
    private readonly float radiusSquared;

    public Circle(in Vector2 origin, float radius) {
        this.origin = origin;
        radiusSquared = radius * radius;
    }

    public virtual bool Contains(Vector2 point) {
        return Vector2.DistanceSquared(origin, point) <= radiusSquared;
    }

    public override string ToString() {
        return $"Origin:{origin}, Radius:{MathF.Sqrt(radiusSquared)}";
    }
}
