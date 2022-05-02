using System.Collections.Generic;
using System.Numerics;

namespace Maple2.Server.Game.Model;

public interface IFieldObject {
    public int ObjectId { get; init; }
}

public interface IFieldEntity : IFieldObject {
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
}

public interface IActor : IFieldEntity {
    public IReadOnlyDictionary<int, Buff> Buffs { get; }

    // Syncs the object to the field
    public virtual void Sync() { }
}

public interface IActor<T> : IActor {
    public T Value { get; init; }
}
