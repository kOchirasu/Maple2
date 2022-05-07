using System.Numerics;

namespace Maple2.Model.Metadata;

public abstract record InteractObject(MapBlock.Discriminator Class, int InteractId, bool MinimapInVisible) : MapBlock(Class);

public record InteractActor(
    int InteractId,
    bool MinimapInVisible,
    Vector3 Position,
    Vector3 Rotation)
: InteractObject(Discriminator.InteractActor, InteractId, MinimapInVisible);
