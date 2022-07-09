using System.Numerics;

namespace Maple2.Model.Metadata;

public record Telescope(
    int Id,
    bool Enabled,
    Vector3 Position,
    Vector3 Rotation
) : MapBlock(Discriminator.Telescope);
