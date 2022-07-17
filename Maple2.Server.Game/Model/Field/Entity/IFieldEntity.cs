using Maple2.Server.Game.Manager.Field;

namespace Maple2.Server.Game.Model;

/// <summary>
/// FieldEntities are active objects that are spawned on the field.
/// </summary>
public interface IFieldEntity : IFieldObject {
    public FieldManager Field { get; }

    // Syncs the object to the field
    public virtual void Sync() { }
}

public interface IFieldEntity<T> : IFieldEntity {
    public T Value { get; }
}
