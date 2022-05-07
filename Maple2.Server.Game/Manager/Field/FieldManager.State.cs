using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
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

    private readonly ConcurrentDictionary<int, FieldPlayer> fieldPlayers = new();
    private readonly ConcurrentDictionary<int, FieldEntity<Item>> fieldItems = new();

    public FieldPlayer SpawnPlayer(GameSession session, Player player, int portalId = 0,
        in Vector3 position = default, in Vector3 rotation = default) {
        // TODO: Not sure what the difference is between instance ids.
        player.Character.MapId = MapId;
        player.Character.InstanceMapId = MapId;
        player.Character.InstanceId = InstanceId;

        int objectId = Interlocked.Increment(ref objectIdCounter);
        var fieldPlayer = new FieldPlayer(objectId, session, player) {
            Position = position,
            Rotation = rotation
        };

        // Use Portal if needed.
        if (fieldPlayer.Position == default && entities.Portals.TryGetValue(portalId, out Portal? portal)) {
            fieldPlayer.Position = portal.Position;
            fieldPlayer.Rotation = portal.Rotation;
        }

        // Use SpawnPoint if needed.
        if (fieldPlayer.Position == default) {
            SpawnPointPC? spawn = entities.PlayerSpawns.Values.FirstOrDefault(spawn => spawn.Enable);
            if (spawn != null) {
                fieldPlayer.Position = spawn.Position;
                fieldPlayer.Rotation = spawn.Rotation;
            }
        }

        if (Metadata.Property.RevivalReturnId != 0) {
            player.Character.ReviveMapId = Metadata.Property.RevivalReturnId;
        }
        if (Metadata.Property.EnterReturnId != 0) {
            player.Character.ReturnMapId = Metadata.Property.EnterReturnId;
        }

        return fieldPlayer;
    }

    public bool RemovePlayer(int objectId, [NotNullWhen(true)] out FieldPlayer? fieldPlayer) {
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

    public bool PickupItem(FieldPlayer looter, int objectId, [NotNullWhen(true)] out FieldEntity<Item>? fieldItem) {
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
        Multicast(FieldPacket.AddPlayer(added.Session), added.Session);
        Multicast(ProxyObjectPacket.AddPlayer(added), added.Session);
        foreach (FieldEntity<Item> fieldItem in fieldItems.Values) {
            added.Session.Send(FieldPacket.DropItem(fieldItem));
        }
        // FieldAddNpc
        // FieldAddPet
        foreach (FieldEntity<Portal> fieldPortal in fieldPortals.Values) {
            added.Session.Send(PortalPacket.Add(fieldPortal));
        }
        // ProxyGameObj
        foreach (FieldPlayer fieldPlayer in fieldPlayers.Values) {
            added.Session.Send(ProxyObjectPacket.AddPlayer(fieldPlayer));
        }
        // RegionSkill (on tick?)
        // Stat
    }
    #endregion Events
}
