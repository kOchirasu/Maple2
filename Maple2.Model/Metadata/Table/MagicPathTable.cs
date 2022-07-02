using System.Collections.Generic;
using System.Numerics;

namespace Maple2.Model.Metadata;

public record MagicPathTable(IReadOnlyDictionary<long, IReadOnlyList<MagicPath>> Entries)  : Table(Discriminator.MagicPathTable);

public record MagicPath(
    bool Align,
    int AlignHeight,
    bool Rotate,
    bool IgnoreAdjust,
    Vector3 Direction,
    Vector3 FireOffset,
    Vector3 FireFixed,
    float Velocity,
    float Distance,
    float RotateZDegree,
    float LifeTime,
    float DelayTime,
    float SpawnTime,
    float DestroyTime);
