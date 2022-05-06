using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Maple2.Model;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Field;

public partial class FieldManager {
    private int objectIdCounter = 10000000;

    private readonly ConcurrentDictionary<int, FieldPlayer> fieldPlayers =
        new ConcurrentDictionary<int, FieldPlayer>();
    private readonly ConcurrentDictionary<int, FieldEntity<Item>> fieldItems =
        new ConcurrentDictionary<int, FieldEntity<Item>>();

    public FieldPlayer SpawnPlayer(GameSession session, Player player) {
        // TODO: Not sure what the difference is between instance ids.
        player.Character.MapId = MapId;
        player.Character.InstanceMapId = MapId;
        player.Character.InstanceId = InstanceId;

        int objectId = Interlocked.Increment(ref objectIdCounter);
        var fieldPlayer = new FieldPlayer(objectId, session, player);

        SpawnPointPC? spawn = entities.PlayerSpawns.Values.FirstOrDefault(spawn => spawn.Enable);
        if (spawn != null) {
            fieldPlayer.Position = spawn.Position;
            fieldPlayer.Rotation = spawn.Rotation;
        }

        return fieldPlayer;
    }

    public bool RemovePlayer(int objectId, out FieldPlayer? fieldPlayer) {
        if (fieldPlayers.TryRemove(objectId, out fieldPlayer)) {
            Multicast(FieldPacket.RemovePlayer(objectId));
            return true;
        }

        return false;
    }

    public void DropItem(IFieldEntity owner, Item item) {
        int objectId = Interlocked.Increment(ref objectIdCounter);
        var fieldItem = new FieldEntity<Item>(objectId, item) {
            Owner = owner,
            Position = owner.Position,
            Rotation = owner.Rotation,
        };
        fieldItems[objectId] = fieldItem;

        Multicast(FieldPacket.DropItem(fieldItem));
    }

    public bool PickupItem(FieldPlayer looter, int objectId, out FieldEntity<Item>? fieldItem) {
        if (!fieldItems.TryRemove(objectId, out fieldItem)) return false;

        if (fieldItem.Value.IsMeso()) {
            Multicast(ItemPickupPacket.PickupMeso(objectId, looter, fieldItem.Value.Amount));
        } else if (fieldItem.Value.IsStamina()) {
            Multicast(ItemPickupPacket.PickupStamina(objectId, looter, fieldItem.Value.Amount));
        } else {
            Multicast(ItemPickupPacket.PickupItem(objectId, looter));
        }

        Multicast(FieldPacket.RemoveItem(objectId));
        return true;
    }

    #region Events
    public void OnAddPlayer(FieldPlayer added) {
        fieldPlayers[added.ObjectId] = added;
        // LOAD:
        // Liftable
        // Breakable
        // InteractObject
        foreach (FieldPlayer fieldPlayer in fieldPlayers.Values) {
            added.Session.Send(FieldPacket.AddPlayer(fieldPlayer.Session));
        }
        Multicast(FieldPacket.AddPlayer(added.Session), added.Session); // FieldAddUser
        foreach (FieldEntity<Item> fieldItem in fieldItems.Values) {
            added.Session.Send(FieldPacket.DropItem(fieldItem));
        }
        // FieldAddNpc
        // FieldAddPet
        // FieldAddPortal
        // ProxyGameObj
        // RegionSkill (on tick?)
        // Stat
    }
    #endregion Events
}
