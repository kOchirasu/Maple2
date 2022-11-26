﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Tools.Collision;

namespace Maple2.Server.Game.Util;

public static class SkillUtils {
    public static Prism GetPrism(this SkillMetadataRange range, in Vector3 position, float angle) {
        if (range.Type == SkillRegion.None) {
            return new Prism(IPolygon.Null, 0, 0);
        }

        var origin = new Vector2(position.X, position.Y);
        IPolygon polygon = range.Type switch {
            SkillRegion.Box => new Rectangle(origin, range.Width + range.RangeAdd.X, range.Distance + range.RangeAdd.Y, angle),
            SkillRegion.Cylinder => new Circle(origin, range.Distance),
            SkillRegion.Frustum => new Trapezoid(origin, range.Width, range.EndWidth, range.Distance, angle),
            SkillRegion.HoleCylinder => new HoleCircle(origin, range.Width, range.EndWidth),
            _ => throw new ArgumentOutOfRangeException($"Invalid range type: {range.Type}"),
        };

        return new Prism(polygon, position.Z, range.Height + range.RangeAdd.Z);
    }

    public static IEnumerable<T> Filter<T>(this Prism prism, IEnumerable<T> entities, int limit = 10) where T : IActor {
        foreach (T entity in entities) {
            if (limit <= 0) {
                yield break;
            }

            if (entity.IsDead) {
                continue;
            }

            IPrism shape = entity.Shape;
            if (prism.Intersects(shape)) {
                limit--;
                yield return entity;
            }
        }
    }

    public static IEnumerable<T> Filter<T>(this Prism[] prisms, IEnumerable<T> entities, int limit = 10, ICollection<IActor>? ignore = null) where T : IActor {
        foreach (T entity in entities) {
            if (limit <= 0) {
                yield break;
            }

            if (entity.IsDead) {
                continue;
            }
            if (ignore != null && ignore.Contains(entity)) {
                continue;
            }

            IPrism shape = entity.Shape;
            foreach (Prism prism in prisms) {
                if (prism.Intersects(shape)) {
                    limit--;
                    yield return entity;
                }
            }
        }
    }
}
