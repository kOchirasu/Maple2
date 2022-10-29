using System;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;
using Maple2.Tools.Collision;

namespace Maple2.Server.Game.Model;

public class FieldNpc : Actor<Npc> {
    public Vector3 Velocity { get; set; }

    public FieldMobSpawn? Owner { get; init; }
    public override IPrism Shape => new Prism(
        new Circle(new Vector2(Position.X, Position.Y), Value.Metadata.Property.Capsule.Radius),
        Position.Z,
        Value.Metadata.Property.Capsule.Height
    );

    public NpcState StateData;
    public int SpawnPointId = -1;

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

    public override Stats Stats { get; }
    public int TargetId = 0;

    public FieldNpc(FieldManager field, int objectId, Npc npc) : base (field, objectId, npc) {
        StateData = new NpcState();
        Stats = new Stats(npc.Metadata.Stat);
        SequenceId = -1;
        SequenceCounter = 1;

        Scheduler.ScheduleRepeated(() => Field.Broadcast(Control()), 1000);
        Scheduler.Start();
    }

    protected virtual ByteWriter Control() => NpcControlPacket.Control(this);
    protected virtual void Remove() => Field.RemoveNpc(ObjectId);

    public override void Sync() {
        base.Sync();
    }

    protected override void OnDeath() {
        foreach (Buff buff in Buffs.Values) {
            buff.Remove();
        }

        Owner?.Despawn(ObjectId);
        Scheduler.Schedule(Remove, (int) (Value.Metadata.Dead.Time * 1000));
    }
}
