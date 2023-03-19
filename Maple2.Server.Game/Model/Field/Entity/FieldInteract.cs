﻿using System;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public class FieldInteract : FieldEntity<InteractObjectMetadata> {
    public readonly string EntityId;

    private long nextTick;
    private long reactTick;

    public InteractType Type => Value.Type;

    private int reactLimit;
    public InteractState State { get; private set; }

    public FieldInteract(FieldManager field, int objectId, string entityId, InteractObjectMetadata value) : base(field, objectId, value) {
        EntityId = entityId;
        State = InteractState.Normal;
        nextTick = Environment.TickCount64;
        reactLimit = Value.ReactCount > 0 ? Value.ReactCount : int.MaxValue;
    }

    public bool React() {
        if (State != InteractState.Reactable) {
            return false;
        }

        reactTick = Environment.TickCount64;
        reactLimit--;
        if (reactLimit > 0) {
            SetState(InteractState.Normal);
            nextTick = reactTick + Value.Time.Reset;
        } else {
            if (Value.Time.Hide > 0) {
                SetState(InteractState.Normal);
                nextTick = reactTick + Value.Time.Hide;
            } else {
                // Immediately hide if no delay.
                SetState(InteractState.Hidden);
                nextTick = 0;
            }
        }

        return true;
    }

    public void SetState(InteractState state) {
        if (State == state) {
            return;
        }

        State = state;
        Field.Broadcast(InteractObjectPacket.Update(this));
    }

    public override void Update(long tickCount) {
        if (nextTick == 0 || tickCount < nextTick) {
            return;
        }

        if (State == InteractState.Normal) {
            SetState(reactLimit > 0 ? InteractState.Reactable : InteractState.Hidden);
        }

        nextTick = 0;
    }
}
