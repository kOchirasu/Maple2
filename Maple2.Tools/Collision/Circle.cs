﻿using System;
using System.Numerics;

namespace Maple2.Tools.Collision;

public class Circle : IPolygon {
    public readonly Vector2 Origin;
    public readonly float Radius;

    public Circle(in Vector2 origin, float radius) {
        this.Origin = origin;
        this.Radius = radius;
    }

    public virtual bool Contains(in Vector2 point) {
        return Vector2.DistanceSquared(Origin, point) <= Radius * Radius;
    }

    public Vector2[] GetAxes(Polygon? other) {
        if (other == null) {
            throw new ArgumentException("Cannot compute Axes for circle without polygon");
        }

        float min = float.MaxValue;
        int result = 0;
        for (int i = 0; i < other.Points.Length; i++) {
            float distanceSquared = Vector2.DistanceSquared(Origin, other.Points[i]);
            if (distanceSquared < min) {
                min = distanceSquared;
                result = i;
            }
        }

        return new[] { other.Points[result] - Origin };
    }

    public Range AxisProjection(Vector2 axis) {
        float centerProjection = Vector2.Dot(axis, Origin);
        return new Range(centerProjection - Radius, centerProjection + Radius);
    }

    public virtual bool Intersects(IPolygon other) {
        return other switch {
            Circle circle => circle.IntersectsCircle(this),
            Polygon polygon => polygon.Intersects(this),
            _ => false,
        };
    }

    // Optimization to avoid type-check when we already know the shape is a circle.
    internal virtual bool IntersectsCircle(Circle other) {
        float maxDistance = Radius + other.Radius;
        return Vector2.DistanceSquared(Origin, other.Origin) <= maxDistance * maxDistance;
    }

    public override string ToString() {
        return $"Origin:{Origin}, Radius:{Radius}";
    }
}
