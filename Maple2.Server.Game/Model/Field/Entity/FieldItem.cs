using Maple2.Model;
using Maple2.Model.Game;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public class FieldItem : FieldEntity<Item>, IOwned {
    public IFieldEntity? Owner { get; init; }

    public FieldItem(FieldManager field, int objectId, Item value) : base(field, objectId, value) { }

    public void Pickup(FieldPlayer looter) {
        if (Value.IsMeso()) {
            Field.Multicast(ItemPickupPacket.PickupMeso(ObjectId, looter, Value.Amount));
        } else if (Value.IsStamina()) {
            Field.Multicast(ItemPickupPacket.PickupStamina(ObjectId, looter, Value.Amount));
        } else {
            Field.Multicast(ItemPickupPacket.PickupItem(ObjectId, looter));
        }
    }

    public override void Sync() {
        // TODO: Despawn item
    }
}
