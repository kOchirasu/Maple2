using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.PathEngine;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Action;
using Maple2.Server.Game.Model.State;
using Maple2.Server.Game.Packets;
using Maple2.Tools;
using Maple2.Tools.Collision;
using Maple2.Tools.Extensions;
using NpcAction = Maple2.Server.Game.Model.Action.NpcAction;

namespace Maple2.Server.Game.Model;

public class FieldNpc : Actor<Npc> {
    private const int ACTION_TICK_INTERVAL = 100;
    private const int CONTROL_TICK_INTERVAL = 1000;

    public Vector3 Velocity { get; set; }
    public required Vector3 Origin { get; init; }

    public FieldMobSpawn? Owner { get; init; }
    public override IPrism Shape => new Prism(
        new Circle(new Vector2(Position.X, Position.Y), Value.Metadata.Property.Capsule.Radius),
        Position.Z,
        Value.Metadata.Property.Capsule.Height
    );

    public readonly AnimationSequence IdleSequence;
    private readonly WeightedSet<string> defaultActions;

    public readonly Agent Agent;
    public NpcState StateData { get; protected set; }
    private NpcAction CurrentAction { get; set; }
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

    private int nextActionTick;
    private int nextControlTick;

    public override Stats Stats { get; }
    public int TargetId = 0;

    public FieldNpc(FieldManager field, int objectId, Agent agent, Npc npc) : base (field, objectId, npc) {
        IdleSequence = npc.Animations.GetValueOrDefault("Idle_A") ?? new AnimationSequence(-1, 1f);
        defaultActions = new WeightedSet<string>();
        foreach (Maple2.Model.Metadata.NpcAction action in Value.Metadata.Action.Actions) {
            defaultActions.Add(action.Name, action.Probability);
        }

        Agent = agent;
        StateData = new NpcState();
        CurrentAction = new DelayAction(this, -1, 1f);
        Stats = new Stats(npc.Metadata.Stat);
        SequenceId = -1;
        SequenceCounter = 1;

        Scheduler.Start();
    }

    protected virtual ByteWriter Control() {
        SequenceCounter++; // Not sure if this even matters
        return NpcControlPacket.Control(this);
    }
    protected virtual void Remove() => Field.RemoveNpc(ObjectId);

    public override bool Sync() {
        if (base.Sync()) {
            return true;
        }

        int tickNow = Environment.TickCount;
        if (tickNow < nextActionTick) {
            return false;
        }

        if (CurrentAction.Sync()) {
            CurrentAction = CurrentAction.NextAction?.Invoke() ?? NextAction();
            Field.Broadcast(Control());
            nextActionTick = tickNow + ACTION_TICK_INTERVAL;
        }

        if (tickNow > nextControlTick) {
            Field.Broadcast(Control());
            nextControlTick = tickNow + CONTROL_TICK_INTERVAL;
        }
        return false;
    }

    private NpcAction NextAction() {
        string actionName = defaultActions.Get();
        if (!Value.Animations.TryGetValue(actionName, out AnimationSequence? sequence)) {
            Logger.Error("Invalid action: {Action} for npc {NpcId}", actionName, Value.Metadata.Id);
            return new DelayAction(this, IdleSequence.Id, 1f);
        }

        switch (actionName) {
            // case "Jump_A":
            //     break;
            // case "Jump_B":
            //     break;
            case { } when actionName.Contains("Idle_"):
                return new DelayAction(this, sequence.Id, sequence.Time);
            case { } when actionName.Contains("Bore_"):
                return new DelayAction(this, sequence.Id, sequence.Time + 1f);
            case { } when actionName.StartsWith("Walk_"):
                (Vector3 start, Vector3 end) = Field.Navigation.FindPath(Agent, Origin, Value.Metadata.Action.MoveArea, maxHeight: 5);
                if (start == end) {
                    // No valid path:
                    // 1. Npc is obstructed in all directions
                    // 2. Randomly generated position had too great of a height difference
                    return new DelayAction(this, IdleSequence.Id, sequence.Time);
                }

                Position = start; // Force position to start position
                Vector3 newRotation = start.Angle2D(end) * Vector3.UnitZ;
                return new RotateAction(this, newRotation - Rotation) {
                    NextAction = () => MoveAction.Walk(this, sequence.Id, distance: (int) Vector3.Distance(start, end)),
                };
            case { } when actionName.StartsWith("Run_"):
                return MoveAction.Run(this, sequence.Id, sequence.Time + 1f);
        }

        Logger.Warning("Unhandled action: {Action} for npc {NpcId}", actionName, Value.Metadata.Id);
        return new DelayAction(this, sequence.Id, 1f);
    }

    protected override void OnDeath() {
        foreach (Buff buff in Buffs.Values) {
            buff.Remove();
        }

        Owner?.Despawn(ObjectId);
        CurrentAction.OnCompleted();
        Scheduler.Schedule(Remove, (int) (Value.Metadata.Dead.Time * 1000));
    }
}
