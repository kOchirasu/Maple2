using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record AnimationMetadata(
    string Model,
    IReadOnlyDictionary<string, AnimationSequence> Sequences);

public record AnimationSequence(
    short Id,
    float Time);
