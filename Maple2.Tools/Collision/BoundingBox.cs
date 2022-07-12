using System;
using System.Collections.Generic;
using System.Numerics;

namespace Maple2.Tools.Collision;

public class BoundingBox : IPolygon {
    IReadOnlyList<Vector2> IPolygon.Points => Array.Empty<Vector2>();
    private readonly float minX;
    private readonly float maxX;
    private readonly float minY;
    private readonly float maxY;

    public BoundingBox(in Vector2 min, in Vector2 max) {
        minX = MathF.Min(min.X, max.X);
        maxX = MathF.Max(min.X, max.X);
        minY = MathF.Min(min.Y, max.Y);
        maxY = MathF.Max(min.Y, max.Y);
    }

    public bool Contains(Vector2 point) {
        return minX <= point.X && point.X <= maxX && minY <= point.Y && point.Y <= maxY;
    }
}
