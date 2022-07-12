using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Maple2.Tools.Collision;

public class Circle : IPolygon {
    private const int RESOLUTION = 12;

    IReadOnlyList<Vector2> IPolygon.Points => points;
    private readonly List<Vector2> points;

    public Circle(in Vector2 origin, float radius) {
        const float step = MathF.Tau / RESOLUTION;

        points = new List<Vector2>(RESOLUTION);
        float angle = 0;
        for (int i = 0; i < RESOLUTION; i++, angle += step) {
            points.Add(new Vector2(
                origin.X + radius * MathF.Cos(angle),
                origin.Y + radius * MathF.Sin(angle)
            ));
        }
    }

    public override string ToString() {
        var builder = new StringBuilder();
        foreach (Vector2 point in points) {
            builder.Append($"({point.X}, {point.Y}), ");
        }

        return builder.ToString();
    }
}
