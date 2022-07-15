using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Tools.Collision;

namespace Maple2.Server.Game.Util;

public static class SkillUtils {
    public static Prism GetPrism(this SkillMetadataRegion range, in Vector3 position, float angle) {
        if (range.Type == SkillRegion.None) {
            return new Prism(IPolygon.Null, 0, 0);
        }

        var origin = new Vector2(position.X, position.Y);
        IPolygon polygon = range.Type switch {
            SkillRegion.Box => new Rectangle(origin, range.Width, range.Distance, angle),
            SkillRegion.Cylinder => new Circle(origin, range.Distance),
            SkillRegion.Frustum => new Trapezoid(origin, range.Width, range.EndWidth, range.Distance, angle),
            SkillRegion.HoleCylinder => new HoleCircle(origin, range.Width, range.EndWidth),
            _ => throw new ArgumentOutOfRangeException($"Invalid range type: {range.Type}"),
        };

        return new Prism(polygon, position.Z, range.Height);
    }

    public static IEnumerable<FieldPlayer> Filter(this Prism prism, IEnumerable<FieldPlayer> players, int limit = 10) {
        foreach (FieldPlayer player in players) {
            if (limit <= 0) {
                yield break;
            }

            if (prism.Contains(player.Position)) {
                limit--;
                yield return player;
            }
        }
    }
}
