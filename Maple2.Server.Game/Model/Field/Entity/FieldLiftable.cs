﻿using System;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public class FieldLiftable : FieldEntity<Liftable> {
    public readonly string EntityId;

    public int Count;
    public LiftableState State;
    public int FinishTick;

    public int RespawnTick { get; private set; }

    public FieldLiftable(FieldManager field, int objectId, string entityId, Liftable value) : base(field, objectId, value) {
        EntityId = entityId;
        Count = Value.ItemStackCount;
        FinishTick = Environment.TickCount + Value.FinishTime;
    }

    public LiftableCube? Pickup() {
        if (Count <= 0 || State != LiftableState.Default) {
            return null;
        }

        Count--;
        if (RespawnTick == 0) {
            RespawnTick = Environment.TickCount + Value.RegenCheckTime;
        }

        if (Count > 0) {
            Field.Multicast(LiftablePacket.Update(this));
        } else {
            State = LiftableState.Respawning;
            Field.Multicast(LiftablePacket.Remove(EntityId));
            Field.Multicast(CubePacket.RemoveCube(ObjectId, Position));
        }

        return new LiftableCube(Value);
    }

    public override void Sync() {
        int ticks = Environment.TickCount;
        if (ticks > FinishTick) {
            Field.RemoveLiftable(EntityId);
            return;
        }

        if (RespawnTick == 0 || ticks < RespawnTick) {
            return;
        }

        Count++;
        if (Count < Value.ItemStackCount) {
            RespawnTick = ticks + Value.RegenCheckTime;
        } else {
            RespawnTick = 0;
        }

        if (Count == 1) {
            State = LiftableState.Default;
            Field.Multicast(LiftablePacket.Add(this));
            //Field.Multicast(CubePacket.PlaceLiftable());
        }
        Field.Multicast(LiftablePacket.Update(this));
    }
}