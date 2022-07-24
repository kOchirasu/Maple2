using System.Numerics;

namespace Maple2.Model.Metadata;

public abstract record InteractObject(
    MapBlock.Discriminator Class,
    int InteractId,
    Vector3 Position,
    Vector3 Rotation,
    float Scale) : MapBlock(Class);

public record Ms2InteractActor(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation,
    float Scale)
: InteractObject(Discriminator.Ms2InteractActor, InteractId, Position, Rotation, Scale);

public record Ms2InteractDisplay(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation,
    float Scale)
: InteractObject(Discriminator.Ms2InteractDisplay, InteractId, Position, Rotation, Scale);

public record Ms2InteractMesh(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation,
    float Scale)
: InteractObject(Discriminator.Ms2InteractMesh, InteractId, Position, Rotation, Scale);

// public record Ms2InteractWebActor(
//     int InteractId,
//     bool MinimapInVisible)
// : InteractObject(Discriminator.Ms2InteractWebActor, InteractId, MinimapInVisible);
//
// public record Ms2InteractWebMesh(
//     int InteractId,
//     bool MinimapInVisible)
// : InteractObject(Discriminator.Ms2InteractWebMesh, InteractId, MinimapInVisible);

public record Ms2SimpleUiObject(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation,
    float Scale)
: InteractObject(Discriminator.Ms2SimpleUiObject, InteractId, Position, Rotation, Scale);

public record Ms2Telescope(
    int InteractId,
    Vector3 Position,
    Vector3 Rotation,
    float Scale)
: InteractObject(Discriminator.Ms2Telescope, InteractId, Position, Rotation, Scale);
