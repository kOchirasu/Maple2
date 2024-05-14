﻿using System;
using System.Numerics;

namespace Maple2.Tools.Collision;

public class BoundingBox : Polygon {
    public override Vector2[] Points => points.Value;

    private readonly Lazy<Vector2[]> points;
    private readonly Vector2 min;
    private readonly Vector2 max;

    public BoundingBox(in Vector2 vector1, in Vector2 vector2) {
        min = Vector2.Min(vector1, vector2);
        max = Vector2.Max(vector1, vector2);

        points = new Lazy<Vector2[]>(() => [
            new Vector2(min.X, min.Y),
            new Vector2(max.X, min.Y),
            new Vector2(max.X, max.Y),
            new Vector2(min.X, max.Y),
        ]);
    }

    public override bool Contains(in Vector2 point) {
        return min.X <= point.X && point.X <= max.X && min.Y <= point.Y && point.Y <= max.Y;
    }

    public override string ToString() {
        return $"Min:{min}, Max:{max}";
    }
}
