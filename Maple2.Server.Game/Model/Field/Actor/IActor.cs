using Maple2.Model.Enum;

namespace Maple2.Server.Game.Model;

public interface IActor : IFieldEntity {
    public ActorState State { get; }
    public ActorSubState SubState { get; }
}

public interface IActor<T> : IActor {
    public T Value { get; }
}
