using System.Numerics;

namespace Maple2.Model.Metadata;

public abstract record SpawnPoint(MapBlock.Discriminator Class, int Id, Vector3 Position, Vector3 Rotation, bool Visible) : MapBlock(Class);

public record SpawnPointPC(
    int Id,
    Vector3 Position,
    Vector3 Rotation,
    bool Visible,
    bool Enable
) : SpawnPoint(Discriminator.SpawnPointPC, Id, Position, Rotation, Visible);

public record SpawnPointNPC(
    int Id,
    Vector3 Position,
    Vector3 Rotation,
    bool Visible,
    bool SpawnOnFieldCreate,
    float SpawnRadius,
    int NpcCount,
    int[] NpcIds,
    int RegenCheckTime,
    MapBlock.Discriminator Class = MapBlock.Discriminator.SpawnPointNPC
) : SpawnPoint(Class, Id, Position, Rotation, Visible);

public record EventSpawnPointNPC(
    int Id,
    Vector3 Position,
    Vector3 Rotation,
    bool Visible,
    bool SpawnOnFieldCreate,
    float SpawnRadius,
    int NpcCount,
    int[] NpcIds,
    int RegenCheckTime,
    int LifeTime,
    string SpawnAnimation
) : SpawnPointNPC(Id, Position, Rotation, Visible, SpawnOnFieldCreate, SpawnRadius, NpcCount, NpcIds, RegenCheckTime, Discriminator.EventSpawnPointNPC);
