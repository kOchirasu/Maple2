using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Server.Game.Model;

public interface IActor : IFieldEntity {
    protected static readonly IReadOnlyDictionary<int, Buff> NoBuffs = new Dictionary<int, Buff>();
    public IReadOnlyDictionary<int, Buff> Buffs { get; }

    public Stats Stats { get; }

    public ActorState State { get; }
    public ActorSubState SubState { get; }
}

public interface IActor<T> : IActor {
    public T Value { get; }
}
