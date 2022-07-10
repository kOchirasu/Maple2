using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Maple2.Database.Storage;
using Maple2.Model.Common;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Trigger;
using Maple2.Trigger;
using Serilog;

namespace Maple2.Server.Game.Manager.Field;

// FieldManager is instantiated by Autofac
// ReSharper disable once ClassNeverInstantiated.Global
public sealed partial class FieldManager : IDisposable {
    private static int globalIdCounter = 10000000;
    private int localIdCounter = 50000000;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public GameStorage GameStorage { get; init; } = null!;
    public NpcMetadataStorage NpcMetadata { private get; init; } = null!;
    public Lua.Lua Lua { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public readonly MapMetadata Metadata;
    private readonly UgcMapMetadata ugcMetadata;
    private readonly MapEntityMetadata entities;

    private readonly ConcurrentBag<SpawnPointNPC> npcSpawns = new();

    private readonly CancellationTokenSource cancel;
    private readonly Thread thread;

    private readonly ILogger logger = Log.Logger.ForContext<FieldManager>();

    public int MapId => Metadata.Id;
    public readonly int InstanceId;

    public FieldManager(MapMetadata metadata, UgcMapMetadata ugcMetadata, MapEntityMetadata entities, int instanceId = 0) {
        Metadata = metadata;
        this.ugcMetadata = ugcMetadata;
        this.entities = entities;

        InstanceId = instanceId;
        cancel = new CancellationTokenSource();
        thread = new Thread(Sync);
    }

    // Init is separate from constructor to allow properties to be injected first.
    private void Init() {
        if (ugcMetadata.Plots.Count > 0) {
            using GameStorage.Request db = GameStorage.Context();
            foreach (Plot plot in db.LoadPlotsForMap(MapId)) {
                Plots[plot.Number] = plot;
            }
        }

        foreach (Portal portal in entities.Portals.Values) {
            SpawnPortal(portal);
        }
        foreach (TriggerModel trigger in entities.TriggerModels.Values) {
            AddTrigger(trigger);
        }
        foreach ((Guid guid, BreakableActor breakable) in entities.BreakableActors) {
            AddBreakable(guid.ToString("N"), breakable);
        }
        foreach ((Guid guid, Liftable liftable) in entities.Liftables) {
            AddLiftable(guid.ToString("N"), liftable);
        }

        foreach (SpawnPointNPC spawnPointNpc in entities.NpcSpawns) {
            if (spawnPointNpc.RegenCheckTime > 0) {
                npcSpawns.Add(spawnPointNpc);
            }

            if (spawnPointNpc.SpawnOnFieldCreate) {
                for (int i = 0; i < spawnPointNpc.NpcCount; i++) {
                    // TODO: get other NpcIds too
                    int npcId = spawnPointNpc.NpcIds[0];
                    if (!NpcMetadata.TryGet(npcId, out NpcMetadata? npc)) {
                        logger.Warning("Npc {NpcId} failed to load for map {MapId}", npcId, MapId);
                        continue;
                    }

                    SpawnNpc(npc, spawnPointNpc.Position, spawnPointNpc.Rotation);
                }
            }
        }

        foreach (MapMetadataSpawn spawn in Metadata.Spawns) {
            if (!entities.RegionSpawns.TryGetValue(spawn.Id, out Ms2RegionSpawn? regionSpawn)) {
                continue;
            }

            var npcIds = new HashSet<int>();
            foreach (string tag in spawn.Tags) {
                if (NpcMetadata.TryLookupTag(tag, out IReadOnlyCollection<int>? tagNpcIds)) {
                    foreach (int tagNpcId in tagNpcIds) {
                        npcIds.Add(tagNpcId);
                    }
                }
            }

            if (npcIds.Count > 0 && spawn.Population > 0) {
                AddMobSpawn(spawn, regionSpawn, npcIds);
            }
        }

        thread.Start();
    }

    /// <summary>
    /// Generates an ObjectId unique across all map instances.
    /// </summary>
    /// <returns>Returns a globally unique ObjectId</returns>
    public static int NextGlobalId() => Interlocked.Increment(ref globalIdCounter);

    /// <summary>
    /// Generates an ObjectId unique to this specific map instance.
    /// </summary>
    /// <returns>Returns a local ObjectId</returns>
    private int NextLocalId() => Interlocked.Increment(ref localIdCounter);

    private void Sync() {
        while (!cancel.IsCancellationRequested) {
            foreach (FieldTrigger trigger in fieldTriggers.Values) trigger.Sync();

            foreach (FieldPlayer player in fieldPlayers.Values) player.Sync();
            foreach (FieldNpc npc in fieldNpcs.Values) npc.Sync();
            foreach (FieldBreakable breakable in fieldBreakables.Values) breakable.Sync();
            foreach (FieldLiftable liftable in fieldLiftables.Values) liftable.Sync();
            foreach (FieldItem item in fieldItems.Values) item.Sync();
            foreach (FieldMobSpawn mobSpawn in fieldMobSpawns.Values) mobSpawn.Sync();
            Thread.Sleep(50);
        }
    }

    public bool TryGetPlayerById(long characterId, [NotNullWhen(true)] out FieldPlayer? player) {
        foreach (FieldPlayer fieldPlayer in fieldPlayers.Values) {
            if (fieldPlayer.Value.Character.Id == characterId) {
                player = fieldPlayer;
                return true;
            }
        }

        player = null;
        return false;
    }

    public bool TryGetPlayer(int objectId, [NotNullWhen(true)] out FieldPlayer? player) {
        return fieldPlayers.TryGetValue(objectId, out player);
    }

    public bool TryGetNpc(int objectId, [NotNullWhen(true)] out FieldNpc? npc) {
        return fieldNpcs.TryGetValue(objectId, out npc);
    }

    public bool TryGetPortal(int portalId, [NotNullWhen(true)] out Portal? portal) {
        return entities.Portals.TryGetValue(portalId, out portal);
    }

    public bool TryGetItem(int objectId, [NotNullWhen(true)] out FieldItem? fieldItem) {
        return fieldItems.TryGetValue(objectId, out fieldItem);
    }

    public bool TryGetTrigger(string name, [NotNullWhen(true)] out FieldTrigger? fieldTrigger) {
        return fieldTriggers.TryGetValue(name, out fieldTrigger);
    }

    public bool TryGetBreakable(string entityId, [NotNullWhen(true)] out FieldBreakable? fieldBreakable) {
        return fieldBreakables.TryGetValue(entityId, out fieldBreakable);
    }

    public bool TryGetLiftable(string entityId, [NotNullWhen(true)] out FieldLiftable? fieldLiftable) {
        return fieldLiftables.TryGetValue(entityId, out fieldLiftable);
    }

    public bool MoveToPortal(GameSession session, int portalId) {
        if (!TryGetPortal(portalId, out Portal? portal)) {
            return false;
        }

        session.Send(PortalPacket.MoveByPortal(session.Player, portal));
        return true;
    }

    public bool UsePortal(GameSession session, int portalId) {
        if (!TryGetPortal(portalId, out Portal? srcPortal)) {
            return false;
        }

        // MoveByPortal (same map)
        if (srcPortal.TargetMapId == MapId) {
            if (TryGetPortal(srcPortal.TargetPortalId, out Portal? dstPortal)) {
                session.Send(PortalPacket.MoveByPortal(session.Player, dstPortal));
            }

            return true;
        }

        if (srcPortal.TargetMapId == 0) {
            session.ReturnField();
            return true;
        }

        session.Send(session.PrepareField(srcPortal.TargetMapId, portalId: srcPortal.TargetPortalId)
            ? FieldEnterPacket.Request(session.Player)
            : FieldEnterPacket.Error(MigrationError.s_move_err_default));
        return true;
    }

    public bool LiftupCube(in Vector3B coordinates, [NotNullWhen(true)] out ObjectWeapon? objectWeapon) {
        if (!entities.ObjectWeapons.TryGetValue(coordinates, out objectWeapon)) {
            return false;
        }

        // TODO: Spawn Npcs
        if (objectWeapon.SpawnNpcId == 0 || Random.Shared.NextSingle() >= objectWeapon.SpawnNpcRate) {
            return true;
        }

        return true;
    }

    public void Broadcast(ByteWriter packet, GameSession? sender = null) {
        foreach (FieldPlayer fieldPlayer in fieldPlayers.Values) {
            if (fieldPlayer.Session == sender) continue;
            fieldPlayer.Session.Send(packet);
        }
    }

    public void Dispose() {
        cancel.Cancel();
        thread.Join();
    }
}
