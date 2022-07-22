using System;
using System.Collections.Generic;
using System.Numerics;

namespace Maple2.Tools.Collision;

public class BoundingBox : IPolygon {
    IReadOnlyList<Vector2> IPolygon.Points => Array.Empty<Vector2>();
    private readonly Vector2 min;
    private readonly Vector2 max;

    public BoundingBox(in Vector2 vector1, in Vector2 vector2) {
        min = Vector2.Min(vector1, vector2);
        max = Vector2.Max(vector1, vector2);
    }

    public bool Contains(Vector2 point) {
        return min.X <= point.X && point.X <= max.X && min.Y <= point.Y && point.Y <= max.Y;
    }

    public override string ToString() {
        return $"Min:{min}, Max:{max}";
    }
}
