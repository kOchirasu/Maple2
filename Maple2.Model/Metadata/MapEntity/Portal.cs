using System.Numerics;

namespace Maple2.Model.Metadata; 

public record Portal(
    int Id, 
    int TargetMapId, 
    int TargetPortalId, 
    uint Type, 
    int ActionType,
    Vector3 Position,
    Vector3 Rotation,
    Vector3 Dimension,
    Vector3 Offset,
    bool MinimapVisible,
    bool Enable
) : MapBlock(Discriminator.Portal);
