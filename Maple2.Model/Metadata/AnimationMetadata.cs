using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record AnimationMetadata(
    string Model,
    Dictionary<int, AnimationSequence> Sequences);

public record AnimationSequence(
    string Name,
    float Time);
