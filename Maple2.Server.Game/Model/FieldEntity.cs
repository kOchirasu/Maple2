using System.Numerics;

namespace Maple2.Server.Game.Model;

public class FieldEntity<T> : IFieldEntity<T> {
    public int ObjectId { get; }
    public IFieldEntity? Owner { get; init; }
    public T Value { get; }

    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public FieldEntity(int objectId, T value) {
        ObjectId = objectId;
        Value = value;
    }

    public static implicit operator T(FieldEntity<T> fieldEntity) => fieldEntity.Value;
}
