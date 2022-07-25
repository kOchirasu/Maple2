using System;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public class FieldBreakable : FieldEntity<BreakableActor> {
    public readonly string EntityId;

    private int nextTick;

    public BreakableState State { get; private set; }
    public int BaseTick { get; private set; }

    private bool visible;
    public bool Visible {
        get => visible;
        set {
            if (!visible && value) {
                BaseTick = Environment.TickCount;
            }
            visible = value;
        }
    }

    public FieldBreakable(FieldManager field, int objectId, string entityId, BreakableActor breakable) : base(field, objectId, breakable) {
        EntityId = entityId;
        visible = breakable.Visible;
        State = breakable.Visible ? BreakableState.Show : BreakableState.Hide;
    }

    public bool UpdateState(BreakableState state) {
        if (State == state) {
            return false;
        }

        State = state;
        Field.Broadcast(BreakablePacket.Update(this));

        nextTick = State switch {
            BreakableState.Show => 0,
            BreakableState.Break => Environment.TickCount + Value.HideTime,
            BreakableState.Hide => Environment.TickCount + Value.ResetTime,
            BreakableState.Unknown5 => 0,
            BreakableState.Unknown6 => 0,
            _ => 0,
        };

        return true;
    }

    public override void Sync() {
        if (nextTick == 0 || Environment.TickCount < nextTick) {
            return;
        }

        switch (State) {
            case BreakableState.Show:
                break;
            case BreakableState.Break:
                UpdateState(BreakableState.Hide);
                break;
            case BreakableState.Hide:
                UpdateState(BreakableState.Show);
                break;
            case BreakableState.Unknown5:
                break;
            case BreakableState.Unknown6:
                break;
        }
    }
}
