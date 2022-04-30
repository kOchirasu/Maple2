using System.Numerics;

namespace Maple2.Model.Metadata;

public abstract record SpawnPoint(MapBlock.Discriminator Class, Vector3 Position, Vector3 Rotation, bool Visible) : MapBlock(Class);

public record SpawnPointPC(
    int Id,
    Vector3 Position,
    Vector3 Rotation,
    bool Visible,
    bool Enable
) : SpawnPoint(Discriminator.SpawnPointPC, Position, Rotation, Visible);
