using System.Collections.Concurrent;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.Server.Game.Model;

public interface IActor : IFieldEntity {
    protected static readonly ConcurrentDictionary<int, Buff> NoBuffs = new();
    public ConcurrentDictionary<int, Buff> Buffs { get; }

    public Stats Stats { get; }

    public bool IsDead { get; }
    public ActorState State { get; }
    public ActorSubState SubState { get; }

    public virtual void ApplyEffect(IActor caster, SkillEffectMetadata effect) { }
}

public interface IActor<T> : IActor {
    public T Value { get; }
}
