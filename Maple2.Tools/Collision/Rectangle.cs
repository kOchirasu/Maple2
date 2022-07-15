using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Maple2.Tools.Collision;

public sealed class Rectangle : IPolygon {
    IReadOnlyList<Vector2> IPolygon.Points => points;
    private readonly List<Vector2> points;

    public Rectangle(in Vector2 origin, float width, float length, float angle) {
        float radians = angle * (MathF.PI / 180);
        float halfWidth = width / 2;

        Matrix3x2 transform = Matrix3x2.CreateRotation(radians) * Matrix3x2.CreateTranslation(origin);

        points = new List<Vector2>(4) {
            Vector2.Transform(new Vector2(-halfWidth, 0), transform),
            Vector2.Transform(new Vector2(halfWidth, 0), transform),
            Vector2.Transform(new Vector2(halfWidth, length), transform),
            Vector2.Transform(new Vector2(-halfWidth, length), transform),
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
