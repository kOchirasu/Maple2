using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Maple2.Tools.Collision;

public class Trapezoid : IPolygon {
    IReadOnlyList<Vector2> IPolygon.Points => points;
    private readonly List<Vector2> points;

    public Trapezoid(in Vector2 origin, float width, float endWidth, float distance, float angle) {
        float radians = angle * (MathF.PI / 180);
        float halfWidth = width / 2;
        float halfEndWidth = endWidth / 2;

        Matrix3x2 transform = Matrix3x2.CreateRotation(radians) * Matrix3x2.CreateTranslation(origin);
        points = new List<Vector2>(4) {
            Vector2.Transform(new Vector2(-halfWidth, 0), transform),
            Vector2.Transform(new Vector2(halfWidth, 0), transform),
            Vector2.Transform(new Vector2(halfEndWidth, distance), transform),
            Vector2.Transform(new Vector2(-halfEndWidth, distance), transform),
        };
    }

    public override string ToString() {
        var builder = new StringBuilder();
        foreach (Vector2 point in points) {
            builder.Append($"({point.X}, {point.Y}), ");
        }

        return builder.ToString();
    }
}
