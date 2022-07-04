using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Server.Game.Manager.Field;
using Maple2.Tools.Scheduler;

namespace Maple2.Server.Game.Model;

public abstract class ActorBase<T> : IActor<T> {
    public FieldManager Field { get; }
    public T Value { get; }

    public int ObjectId { get; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public virtual ActorState State { get; set; }
    public virtual ActorSubState SubState { get; set; }

    protected ActorBase(FieldManager field, int objectId, T value) {
        Field = field;
        ObjectId = objectId;
        Value = value;
    }

    public virtual void Sync() { }
}

/// <summary>
/// Actor is an ActorBase that can engage in combat.
/// </summary>
/// <typeparam name="T">The type contained by this object</typeparam>
public abstract class Actor<T> : ActorBase<T>, IDisposable {
    public abstract IReadOnlyDictionary<int, Buff> Buffs { get; }
    public abstract Stats Stats { get; }

    protected readonly EventQueue Scheduler;

    protected Actor(FieldManager field, int objectId, T value) : base(field, objectId, value) {
        Scheduler = new EventQueue();
    }

    public override void Sync() {
        Scheduler.InvokeAll();
    }

    public void Dispose() {
        Scheduler.Stop();
    }
}
