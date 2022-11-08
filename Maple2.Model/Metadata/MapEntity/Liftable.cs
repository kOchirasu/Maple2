﻿using System.Numerics;

namespace Maple2.Model.Metadata;

public record Liftable(
    int ItemId,
    int ItemStackCount,
    int ItemLifetime,
    int RegenCheckTime,
    int FinishTime,
    string MaskQuestId,
    string MaskQuestState,
    string EffectQuestId,
    string EffectQuestState,
    Vector3 Position,
    Vector3 Rotation)
: MapBlock;
