using System;
using System.Collections.Generic;
using System.Numerics;

namespace Maple2.Tools.Collision;

internal class NullPolygon : IPolygon {
    IReadOnlyList<Vector2> IPolygon.Points => Array.Empty<Vector2>();

    public bool Contains(Vector2 point) => false;
}
