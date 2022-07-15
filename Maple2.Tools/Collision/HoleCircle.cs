using System.Numerics;

namespace Maple2.Tools.Collision;

public class HoleCircle : Circle {
    private readonly Circle hole;

    public HoleCircle(in Vector2 origin, float innerRadius, float outerRadius) : base(origin, outerRadius) {
        hole = new Circle(origin, innerRadius);
    }

    public override bool Contains(Vector2 point) {
        return base.Contains(point) && !hole.Contains(point);
    }

    public override string ToString() {
        return $"Outer:{base.ToString()}, Inner:{hole}";
    }
}
