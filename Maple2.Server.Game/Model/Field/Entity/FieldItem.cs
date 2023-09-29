using System;
using Maple2.Model;
using Maple2.Model.Game;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public class FieldItem : FieldEntity<Item> {
    public IActor? Owner { get; init; }

    private readonly long despawnTick;

    public FieldItem(FieldManager field, int objectId, Item value) : base(field, objectId, value) {
        despawnTick = Environment.TickCount64 + (int) TimeSpan.FromMinutes(2).TotalMilliseconds;
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
