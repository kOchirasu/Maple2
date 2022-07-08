using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Manager.Field;

public partial class FieldManager {
    // Actors
    private readonly ConcurrentDictionary<int, FieldPlayer> fieldPlayers = new();
    private readonly ConcurrentDictionary<int, FieldNpc> fieldNpcs = new();

    // Entities
    private readonly ConcurrentDictionary<string, FieldBreakable> fieldBreakables = new();
    private readonly ConcurrentDictionary<int, FieldItem> fieldItems = new();
    private readonly ConcurrentDictionary<int, FieldMobSpawn> fieldMobSpawns = new();

    // Objects
    private readonly ConcurrentDictionary<int, FieldObject<Portal>> fieldPortals = new();

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

    public FieldObject<Portal> SpawnPortal(Portal portal, Vector3 position = default, Vector3 rotation = default) {
        var fieldPortal = new FieldObject<Portal>(NextLocalId(), portal) {
            Position = position != default ? position : portal.Position,
            Rotation = rotation != default ? rotation : portal.Rotation,
        };
        fieldPortals[fieldPortal.ObjectId] = fieldPortal;

        return fieldPortal;
    }

    public FieldItem SpawnItem(IFieldEntity owner, Item item) {
        var fieldItem = new FieldItem(this, NextLocalId(), item) {
            Owner = owner,
            Position = owner.Position,
            Rotation = owner.Rotation,
        };
        fieldItems[fieldItem.ObjectId] = fieldItem;

        return fieldItem;
    }

    // GuideObject is not added to the field, it will be managed by |GameSession.State|
    public FieldGuideObject SpawnGuideObject(IActor<Player> owner, IGuideObject guideObject) {
        var fieldGuideObject = new FieldGuideObject(this, NextLocalId(), guideObject) {
            CharacterId = owner.Value.Character.Id,
            Position = owner.Position,
            // rotation?
        };

        return fieldGuideObject;
    }

    public FieldBreakable AddBreakable(string entityId, BreakableActor breakable) {
        var fieldBreakable = new FieldBreakable(this, NextLocalId(), entityId, breakable) {
            Position = breakable.Position,
            Rotation = breakable.Rotation,
        };

        fieldBreakables[entityId] = fieldBreakable;
        return fieldBreakable;
    }

    public FieldMobSpawn? AddMobSpawn(MapMetadataSpawn metadata, RegionSpawn regionSpawn, ICollection<int> npcIds) {
        var spawnNpcs = new WeightedSet<NpcMetadata>();
        foreach (int npcId in npcIds) {
            if (!NpcMetadata.TryGet(npcId, out NpcMetadata? npc)) {
                continue;
            }
            if (npc.Basic.Difficulty < metadata.MinDifficulty || npc.Basic.Difficulty > metadata.MaxDifficulty) {
                continue;
            }

            int spawnWeight = Lua.CalcNpcSpawnWeight(npc.Basic.MainTags.Length, npc.Basic.SubTags.Length, npc.Basic.RareDegree, npc.Basic.Difficulty);
            spawnNpcs.Add(npc, spawnWeight);
        }

        if (spawnNpcs.Count <= 0) {
            logger.Warning("No valid Npcs found from: {NpcIds}", string.Join(",", npcIds));
            return null;
        }

        var fieldMobSpawn = new FieldMobSpawn(this, NextLocalId(), metadata, spawnNpcs) {
            Position = regionSpawn.Position,
            Rotation = regionSpawn.UseRotAsSpawnDir ? regionSpawn.Rotation : default,
        };

        fieldMobSpawns[fieldMobSpawn.ObjectId] = fieldMobSpawn;
        return fieldMobSpawn;
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

    public bool PickupItem(FieldPlayer looter, int objectId, [NotNullWhen(true)] out Item? item) {
        if (!fieldItems.TryRemove(objectId, out FieldItem? fieldItem)) {
            item = null;
            return false;
        }

        item = fieldItem.Value;
        fieldItem.Pickup(looter);
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
        foreach (FieldItem fieldItem in fieldItems.Values) {
            added.Session.Send(FieldPacket.DropItem(fieldItem));
        }
        foreach (FieldNpc fieldNpc in fieldNpcs.Values) {
            added.Session.Send(FieldPacket.AddNpc(fieldNpc));
        }
        // FieldAddPet
        foreach (FieldObject<Portal> fieldPortal in fieldPortals.Values) {
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

    public bool UpdatePlot(PlotInfo plotInfo) {
        if (MapId != plotInfo.MapId || !Plots.ContainsKey(plotInfo.Number)) {
            return false;
        }

        if (plotInfo is Plot plot) {
            Plots[plot.Number] = plot;
        } else {
            plot = Plots[plotInfo.Number];
            plot.OwnerId = plotInfo.OwnerId;
            plot.Name = plotInfo.Name;
            plot.ExpiryTime = plotInfo.ExpiryTime;
        }

        Multicast(CubePacket.UpdatePlot(plot));
        return true;
    }
}
