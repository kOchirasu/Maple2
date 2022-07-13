using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Maple2.Tools.Collision;

// Polygon is assumed to be convex.
public interface IPolygon {
    public static readonly IPolygon Null = new NullPolygon();

    protected IReadOnlyList<Vector2> Points { get; }

    public bool Contains(float x, float y) => Contains(new Vector2(x, y));

    public virtual bool Contains(Vector2 point) {
        // Check if a triangle or higher n-gon
        Debug.Assert(Points.Count >= 3);

        // n>2 Keep track of cross product sign changes
        int pos = 0;
        int neg = 0;

        for (int i = 0; i < Points.Count; i++) {
            // If point is in the polygon
            if (Points[i] == point) {
                return true;
            }

            // Form a segment between the i'th point
            float x1 = Points[i].X;
            float y1 = Points[i].Y;

            // And the i+1'th, or if i is the last, with the first point
            int i2 = (i + 1) % Points.Count;

            float x = point.X;
            float x2 = Points[i2].X;

            float y = point.Y;
            float y2 = Points[i2].Y;

            // Compute the cross product
            float d = (x - x1) * (y2 - y1) - (y - y1) * (x2 - x1);
            switch (d) {
                case > 0:
                    pos++;
                    break;
                case < 0:
                    neg++;
                    break;
            }

            // If the sign changes, then point is outside
            if (pos > 0 && neg > 0) {
                return false;
            }
        }

        // If no change in direction, then on same side of all segments, and thus inside
        return true;
    }
}
