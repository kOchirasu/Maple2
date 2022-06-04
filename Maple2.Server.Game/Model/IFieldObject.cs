using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Server.Game.Manager.Field;

namespace Maple2.Server.Game.Model;

public interface IFieldObject {
    public int ObjectId { get; }
}

public interface IFieldEntity : IFieldObject {
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
}

public interface IFieldEntity<out T> : IFieldEntity {
    IFieldEntity? Owner { get; init; }
    public T Value { get; }
}

public interface IActor : IFieldEntity {
    public FieldManager Field { get; }
    public IReadOnlyDictionary<int, Buff> Buffs { get; }
    public Stats Stats { get; }

    // Syncs the object to the field
    public virtual void Sync() { }
}

public interface IActor<T> : IActor {
    public T Value { get; }

    public ActorState State { get; }
    public ActorSubState SubState { get; }
}
