using System;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;

namespace Maple2.Server.Game.Model;

public class FieldLiftable : FieldEntity<Liftable> {
    public readonly string EntityId;

    public LiftableState State { get; private set; }
    public int Count { get; private set; }
    public int RespawnTick { get; private set; }

    public FieldLiftable(FieldManager field, int objectId, string entityId, Liftable value) : base(field, objectId, value) {
        EntityId = entityId;
        Count = Value.StackCount;
    }

    public bool Pickup() {
        if (Count <= 0) {
            return false;
        }

        Count--;
        if (RespawnTick == 0) {
            RespawnTick = Environment.TickCount + Value.RegenCheckTime;
        }

        return true;
    }

    public override void Sync() {
        int ticks = Environment.TickCount;
        if (RespawnTick == 0 || ticks < RespawnTick) {
            return;
        }

        Count++;
        if (Count < Value.StackCount) {
            RespawnTick = ticks + Value.RegenCheckTime;
        } else {
            RespawnTick = 0;
        }
    }
}
