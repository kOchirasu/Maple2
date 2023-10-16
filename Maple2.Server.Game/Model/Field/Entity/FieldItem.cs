﻿using System;
using Maple2.Model;
using Maple2.Model.Game;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public class FieldItem : FieldEntity<Item> {
    public IFieldEntity? Owner { get; init; }

    private readonly long despawnTick;
    public bool FixedPosition { get; init; }

    public FieldItem(FieldManager field, int objectId, Item value, long msDuration = 0) : base(field, objectId, value) {
        despawnTick = msDuration == 0 ? Environment.TickCount64 + (int) TimeSpan.FromMinutes(2).TotalMilliseconds :
            Environment.TickCount64 + (int) msDuration;
    }

    public void Pickup(FieldPlayer looter) {
        if (Value.IsMeso()) {
            Field.Broadcast(ItemPickupPacket.PickupMeso(ObjectId, looter, Value.Amount));
        } else if (Value.IsStamina()) {
            Field.Broadcast(ItemPickupPacket.PickupStamina(ObjectId, looter, Value.Amount));
        } else {
            Field.Broadcast(ItemPickupPacket.PickupItem(ObjectId, looter));
        }
    }

    public override void Update(long tickCount) {
        if (tickCount > despawnTick) {
            Field.RemoveItem(ObjectId);
        }
    }
}
