using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading;
using Maple2.Database.Storage;
using Maple2.Model.Common;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.PathEngine;
using Maple2.PathEngine.Interface;
using Maple2.PathEngine.Types;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Util;
using Maple2.Tools.Extensions;
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
    public ItemMetadataStorage ItemMetadata { get; init; } = null!;
    public NpcMetadataStorage NpcMetadata { get; init; } = null!;
    public SkillMetadataStorage SkillMetadata { get; init; } = null!;
    public TableMetadataStorage TableMetadata { get; init; } = null!;
    public ItemStatsCalculator ItemStatsCalc { get; init; } = null!;
    public Lua.Lua Lua { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public readonly MapMetadata Metadata;
    public readonly MapEntityMetadata Entities;
    public readonly Navigation Navigation;
    private readonly UgcMapMetadata ugcMetadata;

    private readonly ConcurrentBag<SpawnPointNPC> npcSpawns = new();

    internal readonly FieldActor FieldActor;
    private readonly CancellationTokenSource cancel;
    private readonly Thread thread;
    private bool initialized = false;

    private readonly ILogger logger = Log.Logger.ForContext<FieldManager>();

    public int MapId => Metadata.Id;
    public readonly int InstanceId;

    public FieldManager(MapMetadata metadata, UgcMapMetadata ugcMetadata, MapEntityMetadata entities, int instanceId = 0) {
        Metadata = metadata;
        this.ugcMetadata = ugcMetadata;
        this.Entities = entities;
        TriggerObjects = new TriggerCollection(entities);

        InstanceId = instanceId;
        FieldActor = new FieldActor(this);
        cancel = new CancellationTokenSource();
        thread = new Thread(Sync);

        Navigation = new Navigation(metadata.XBlock, entities.NavMesh?.Data);
    }

    // Init is separate from constructor to allow properties to be injected first.
    private void Init() {
        if (ugcMetadata.Plots.Count > 0) {
            using GameStorage.Request db = GameStorage.Context();
            foreach (Plot plot in db.LoadPlotsForMap(MapId)) {
                Plots[plot.Number] = plot;
            }
        }

        foreach (TriggerModel trigger in Entities.TriggerModels.Values) {
            AddTrigger(trigger);
        }
        foreach (Portal portal in Entities.Portals.Values) {
            SpawnPortal(portal);
        }
        foreach ((Guid guid, BreakableActor breakable) in Entities.BreakableActors) {
            AddBreakable(guid.ToString("N"), breakable);
        }
        foreach ((Guid guid, Liftable liftable) in Entities.Liftables) {
            AddLiftable(guid.ToString("N"), liftable);
        }
        foreach ((Guid guid, InteractObject interact) in Entities.Interacts) {
            AddInteract(guid.ToString("N"), interact);
        }

        foreach (SpawnPointNPC spawnPointNpc in Entities.NpcSpawns) {
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
            if (!Entities.RegionSpawns.TryGetValue(spawn.Id, out Ms2RegionSpawn? regionSpawn)) {
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

        foreach (Ms2RegionSkill regionSkill in Entities.RegionSkills) {
            if (!SkillMetadata.TryGet(regionSkill.SkillId, regionSkill.Level, out SkillMetadata? skill)) {
                continue;
            }

            AddSkill(skill, regionSkill.Interval, regionSkill.Position, regionSkill.Rotation);
        }

        initialized = true;
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
            if (Players.IsEmpty) {
                Thread.Sleep(1000);
                continue;
            }

            foreach (FieldTrigger trigger in fieldTriggers.Values) trigger.Sync();

            foreach (FieldPlayer player in Players.Values) player.Sync();
            foreach (FieldNpc npc in Npcs.Values) npc.Sync();
            foreach (FieldNpc mob in Mobs.Values) mob.Sync();
            foreach (FieldPet pet in Pets.Values) pet.Sync();
            foreach (FieldBreakable breakable in fieldBreakables.Values) breakable.Sync();
            foreach (FieldLiftable liftable in fieldLiftables.Values) liftable.Sync();
            foreach (FieldInteract interact in fieldInteracts.Values) interact.Sync();
            foreach (FieldItem item in fieldItems.Values) item.Sync();
            foreach (FieldMobSpawn mobSpawn in fieldMobSpawns.Values) mobSpawn.Sync();
            foreach (FieldSkill skill in fieldSkills.Values) skill.Sync();
            Thread.Sleep(50);
        }
    }

    public void EnsurePlayerPosition(FieldPlayer player) {
        if (Entities.BoundingBox.Contains(player.Position)) {
            return;
        }

        player.Session.Send(PortalPacket.MoveByPortal(player, player.LastGroundPosition.Align() + new Vector3(0, 0, 150f), default));
    }

    public bool TryGetPlayerById(long characterId, [NotNullWhen(true)] out FieldPlayer? player) {
        foreach (FieldPlayer fieldPlayer in Players.Values) {
            if (fieldPlayer.Value.Character.Id == characterId) {
                player = fieldPlayer;
                return true;
            }
        }

        player = null;
        return false;
    }

    public bool TryGetPlayer(int objectId, [NotNullWhen(true)] out FieldPlayer? player) {
        return Players.TryGetValue(objectId, out player);
    }

    public bool TryGetPortal(int portalId, [NotNullWhen(true)] out FieldPortal? portal) {
        portal = fieldPortals.Values.FirstOrDefault(p => p.Value.Id == portalId);
        return portal != null;
    }

    public bool TryGetItem(int objectId, [NotNullWhen(true)] out FieldItem? fieldItem) {
        return fieldItems.TryGetValue(objectId, out fieldItem);
    }

    public bool TryGetBreakable(string entityId, [NotNullWhen(true)] out FieldBreakable? fieldBreakable) {
        return fieldBreakables.TryGetValue(entityId, out fieldBreakable);
    }

    public bool TryGetBreakable(int triggerId, [NotNullWhen(true)] out FieldBreakable? fieldBreakable) {
        return triggerBreakable.TryGetValue(triggerId, out fieldBreakable);
    }

    public bool TryGetLiftable(string entityId, [NotNullWhen(true)] out FieldLiftable? fieldLiftable) {
        return fieldLiftables.TryGetValue(entityId, out fieldLiftable);
    }

    public ICollection<FieldInteract> EnumerateInteract() => fieldInteracts.Values;
    public bool TryGetInteract(string entityId, [NotNullWhen(true)] out FieldInteract? fieldInteract) {
        return fieldInteracts.TryGetValue(entityId, out fieldInteract);
    }

    public bool MoveToPortal(GameSession session, int portalId) {
        if (!TryGetPortal(portalId, out FieldPortal? portal)) {
            return false;
        }

        session.Send(PortalPacket.MoveByPortal(session.Player, portal));
        return true;
    }

    public bool UsePortal(GameSession session, int portalId) {
        if (!TryGetPortal(portalId, out FieldPortal? fieldPortal)) {
            return false;
        }

        if (!fieldPortal.Enabled) {
            session.Send(NoticePacket.MessageBox(new InterfaceText($"Cannot use disabled portal: {portalId}")));
            return false;
        }

        // MoveByPortal (same map)
        Portal srcPortal = fieldPortal;
        if (srcPortal.TargetMapId == MapId) {
            if (TryGetPortal(srcPortal.TargetPortalId, out FieldPortal? dstPortal)) {
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

    public bool LiftupCube(in Vector3B coordinates, [NotNullWhen(true)] out LiftupWeapon? liftupWeapon) {
        if (!Entities.ObjectWeapons.TryGetValue(coordinates, out ObjectWeapon? objectWeapon)) {
            liftupWeapon = null;
            return false;
        }

        int itemId = objectWeapon.ItemIds[Environment.TickCount % objectWeapon.ItemIds.Length];
        if (!ItemMetadata.TryGet(itemId, out ItemMetadata? item) || !(item.Skill?.WeaponId > 0)) {
            liftupWeapon = null;
            return false;
        }

        liftupWeapon = new LiftupWeapon(objectWeapon, itemId, item.Skill.WeaponId, item.Skill.WeaponLevel);

        // TODO: Spawn Npcs
        if (objectWeapon.SpawnNpcId == 0 || Random.Shared.NextSingle() >= objectWeapon.SpawnNpcRate) {
            return true;
        }

        return true;
    }

    public void Broadcast(ByteWriter packet, GameSession? sender = null) {
        if (!initialized) {
            return;
        }

        foreach (FieldPlayer fieldPlayer in Players.Values) {
            if (fieldPlayer.Session == sender) continue;
            fieldPlayer.Session.Send(packet);
        }
    }

    public void Dispose() {
        cancel.Cancel();
        cancel.Dispose();
        thread.Join();
        Navigation.Dispose();
    }
}
