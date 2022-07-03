using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Maple2.Model;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Manager.Field;

public partial class FieldManager {
    private readonly ConcurrentDictionary<int, FieldPlayer> fieldPlayers = new();
    private readonly ConcurrentDictionary<int, FieldNpc> fieldNpcs = new();
    private readonly ConcurrentDictionary<int, FieldEntity<Portal>> fieldPortals = new();
    private readonly ConcurrentDictionary<int, FieldEntity<Item>> fieldItems = new();
    private readonly ConcurrentDictionary<string, FieldBreakable> fieldBreakables = new();

    #region Spawn
    public FieldPlayer SpawnPlayer(GameSession session, Player player, int portalId = -1,
        in Vector3 position = default, in Vector3 rotation = default) {
        // TODO: Not sure what the difference is between instance ids.
        player.Character.MapId = MapId;
        player.Character.InstanceMapId = MapId;
        player.Character.InstanceId = InstanceId;

        var fieldPlayer = new FieldPlayer(session, player) {
            Position = position,
            Rotation = rotation,
        };

        // Use Portal if needed.
        if (fieldPlayer.Position == default && entities.Portals.TryGetValue(portalId, out Portal? portal)) {
            fieldPlayer.Position = portal.Position.Offset(portal.FrontOffset, portal.Rotation);
            fieldPlayer.Rotation = portal.Rotation;
        }

        // Use SpawnPoint if needed.
        if (fieldPlayer.Position == default) {
            SpawnPointPC? spawn = entities.PlayerSpawns.Values.FirstOrDefault(spawn => spawn.Enable);
            if (spawn != null) {
                fieldPlayer.Position = spawn.Position + new Vector3(0, 0, 25);
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

    public FieldNpc SpawnNpc(NpcMetadata npc, Vector3 position, Vector3 rotation) {
        var fieldNpc = new FieldNpc(this, NextLocalId(), new Npc(npc)) {
            Position = position,
            Rotation = rotation,
        };
        fieldNpcs[fieldNpc.ObjectId] = fieldNpc;

        return fieldNpc;
    }

    public FieldEntity<Portal> SpawnPortal(Portal portal, Vector3 position = default, Vector3 rotation = default) {
        var fieldPortal = new FieldEntity<Portal>(NextLocalId(), portal) {
            Position = position != default ? position : portal.Position,
            Rotation = rotation != default ? rotation : portal.Rotation,
        };
        fieldPortals[fieldPortal.ObjectId] = fieldPortal;

        return fieldPortal;
    }

    public FieldEntity<Item> SpawnItem(IFieldEntity owner, Item item) {
        var fieldItem = new FieldEntity<Item>(NextLocalId(), item) {
            Owner = owner,
            Position = owner.Position,
            Rotation = owner.Rotation,
        };
        fieldItems[fieldItem.ObjectId] = fieldItem;

        return fieldItem;
    }

    public FieldBreakable SpawnBreakable(string entityId, BreakableActor breakable) {
        var fieldBreakable = new FieldBreakable(this, NextLocalId(), entityId, breakable) {
            Position = breakable.Position,
            Rotation = breakable.Rotation,
        };

        fieldBreakables[entityId] = fieldBreakable;
        return fieldBreakable;
    }
    #endregion

    #region Remove
    public bool RemovePlayer(int objectId, [NotNullWhen(true)] out FieldPlayer? fieldPlayer) {
        if (fieldPlayers.TryRemove(objectId, out fieldPlayer)) {
            Multicast(FieldPacket.RemovePlayer(objectId));
            return true;
        }

        return false;
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
    #endregion

    #region Events
    public void OnAddPlayer(FieldPlayer added) {
        fieldPlayers[added.ObjectId] = added;
        // LOAD:
        // Liftable
        added.Session.Send(BreakablePacket.BatchUpdate(fieldBreakables.Values));
        // InteractObject
        foreach (FieldPlayer fieldPlayer in fieldPlayers.Values) {
            added.Session.Send(FieldPacket.AddPlayer(fieldPlayer.Session));
        }
        Multicast(FieldPacket.AddPlayer(added.Session), added.Session);
        Multicast(ProxyObjectPacket.AddPlayer(added), added.Session);
        foreach (FieldEntity<Item> fieldItem in fieldItems.Values) {
            added.Session.Send(FieldPacket.DropItem(fieldItem));
        }
        foreach (FieldNpc fieldNpc in fieldNpcs.Values) {
            added.Session.Send(FieldPacket.AddNpc(fieldNpc));
        }
        // FieldAddPet
        foreach (FieldEntity<Portal> fieldPortal in fieldPortals.Values) {
            added.Session.Send(PortalPacket.Add(fieldPortal));
        }
        // ProxyGameObj
        foreach (FieldPlayer fieldPlayer in fieldPlayers.Values) {
            added.Session.Send(ProxyObjectPacket.AddPlayer(fieldPlayer));
        }
        foreach (FieldNpc fieldNpc in fieldNpcs.Values) {
            added.Session.Send(ProxyObjectPacket.AddNpc(fieldNpc));
        }

        // RegionSkill (on tick?)
        added.Session.Send(StatsPacket.Init(added));
        Multicast(StatsPacket.Update(added), added.Session);
    }
    #endregion Events
}
