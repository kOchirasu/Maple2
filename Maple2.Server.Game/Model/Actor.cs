using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Server.Game.Manager.Field;
using Maple2.Tools.Scheduler;

namespace Maple2.Server.Game.Model;

public abstract class Actor<T> : IActor<T>, IDisposable {
    public FieldManager Field { get; }
    public T Value { get; }

    public int ObjectId { get; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public ActorState State { get; set; }
    public ActorSubState SubState { get; set; }

    public abstract IReadOnlyDictionary<int, Buff> Buffs { get; }
    public abstract Stats Stats { get; }

    protected readonly EventQueue Scheduler;

    protected Actor(FieldManager field, int objectId, T value) {
        Field = field;
        ObjectId = objectId;
        Value = value;
        Scheduler = new EventQueue();
    }

    public virtual void Sync() {
        Scheduler.InvokeAll();
    }

    public void Dispose() {
        Scheduler.Stop();
    }
}
