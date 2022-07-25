using System.Numerics;

namespace Maple2.Model.Metadata;

public abstract record InteractObject(
    MapBlock.Discriminator Class,
    int InteractId,
    Vector3 Position,
    Vector3 Rotation) : MapBlock(Class);

public record Ms2InteractActor(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation)
: InteractObject(Discriminator.Ms2InteractActor, InteractId, Position, Rotation);

public record Ms2InteractDisplay(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation)
: InteractObject(Discriminator.Ms2InteractDisplay, InteractId, Position, Rotation);

public record Ms2InteractMesh(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation)
: InteractObject(Discriminator.Ms2InteractMesh, InteractId, Position, Rotation);

// These are unused in XML
// public record Ms2InteractWebActor(
//     Vector3 Position,
//     Vector3 Rotation)
// : InteractObject(Discriminator.Ms2InteractWebActor, InteractId, Position, Rotation);
//
// public record Ms2InteractWebMesh(
//     int InteractId,
//     Vector3 Position,
//     Vector3 Rotation)
// : InteractObject(Discriminator.Ms2InteractWebMesh, InteractId, Position, Rotation);

public record Ms2SimpleUiObject(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation)
: InteractObject(Discriminator.Ms2SimpleUiObject, InteractId, Position, Rotation);

public record Ms2Telescope(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation)
: InteractObject(Discriminator.Ms2Telescope, InteractId, Position, Rotation);
