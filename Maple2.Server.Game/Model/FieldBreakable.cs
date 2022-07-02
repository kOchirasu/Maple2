using System;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public class FieldBreakable : ActorBase<BreakableActor> {
    private readonly FieldManager field;
    public readonly string EntityId;
    private int NextTick;

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
        this.field = field;

        EntityId = entityId;
        visible = breakable.Visible;
        State = breakable.Visible ? BreakableState.Show : BreakableState.Hide;
    }

    public bool UpdateState(BreakableState state) {
        if (State == state) {
            return false;
        }

        State = state;
        field.Multicast(BreakablePacket.Update(this));

        switch (State) {
            case BreakableState.Show:
                NextTick = 0;
                break;
            case BreakableState.Break:
                NextTick = Environment.TickCount + Value.HideTime / 2;
                break;
            case BreakableState.Hide:
                NextTick = Environment.TickCount + Value.ResetTime / 4;
                break;
            case BreakableState.Unknown5:
                break;
            case BreakableState.Unknown6:
                break;
        }

        return true;
    }

    public override void Sync() {
        int ticks = Environment.TickCount;
        if (NextTick == 0 || ticks < NextTick) {
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
