using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public class FieldNpc : Actor<Npc> {
    public Vector3 Velocity { get; set; }

    public NpcState StateData;

    public override ActorState State {
        get => StateData.State;
        set => throw new InvalidOperationException("Cannot set Npc State");
    }

    public override ActorSubState SubState {
        get => StateData.SubState;
        set => throw new InvalidOperationException("Cannot set Npc SubState");
    }

    public short SequenceId;
    public short SequenceCounter;

    public override IReadOnlyDictionary<int, Buff> Buffs { get; }
    public override Stats Stats { get; }
    public int TargetId = 0;

    public FieldNpc(FieldManager field, int objectId, Npc npc) : base (field, objectId, npc) {
        StateData = new NpcState();
        Buffs = new Dictionary<int, Buff>();
        Stats = new Stats(npc.Metadata.Stat);
        SequenceId = -1;
        SequenceCounter = 1;

        Scheduler.ScheduleRepeated(() => Field.Broadcast(NpcControlPacket.Control(this)), 1000);
        Scheduler.Start();
    }

    public override void Sync() {
        base.Sync();
    }
}
