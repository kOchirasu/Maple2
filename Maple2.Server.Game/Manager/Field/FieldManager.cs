using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Manager.Field;

public sealed partial class FieldManager : IDisposable {
    public readonly MapMetadata Metadata;
    private readonly MapEntityMetadata entities;
    private readonly NpcMetadataStorage npcMetadata;

    private readonly ConcurrentBag<SpawnPointNPC> npcSpawns = new();

    private readonly ConcurrentDictionary<int, FieldEntity<Portal>> fieldPortals = new();
    private readonly ConcurrentDictionary<int, FieldNpc> fieldNpcs = new();

    private readonly ILogger logger;

    public int MapId => Metadata.Id;
    public readonly int InstanceId;

    private FieldManager(int instanceId, MapMetadata metadata, MapEntityMetadata entities, NpcMetadataStorage npcMetadata, ILogger logger) {
        InstanceId = instanceId;
        this.Metadata = metadata;
        this.entities = entities;
        this.npcMetadata = npcMetadata;
        this.logger = logger;

        foreach (Portal portal in entities.Portals.Values) {
            SpawnPortal(portal);
        }

        foreach (SpawnPointNPC spawnPointNpc in entities.NpcSpawns) {
            if (spawnPointNpc.RegenCheckTime > 0) {
                npcSpawns.Add(spawnPointNpc);
            }

            if (spawnPointNpc.SpawnOnFieldCreate) {
                for (int i = 0; i < spawnPointNpc.NpcCount; i++) {
                    // TODO: get other NpcIds too
                    int npcId = spawnPointNpc.NpcIds[0];
                    if (!npcMetadata.TryGet(npcId, out NpcMetadata? npc)) {
                        logger.LogWarning("Npc {NpcId} failed to load for map {MapId}", npcId, MapId);
                        continue;
                    }

                    SpawnNpc(npc, spawnPointNpc.Position, spawnPointNpc.Rotation);
                }
            }
        }
    }

    public FieldEntity<Portal> SpawnPortal(Portal portal, Vector3 position = default, Vector3 rotation = default) {
        int objectId = Interlocked.Increment(ref objectIdCounter);
        var fieldPortal = new FieldEntity<Portal>(objectId, portal) {
            Position = position != default ? position : portal.Position,
            Rotation = rotation != default ? rotation : portal.Rotation,
        };
        fieldPortals[objectId] = fieldPortal;

        return fieldPortal;
    }

    public bool TryGetPortal(int portalId, [NotNullWhen(true)] out Portal? portal) {
        return entities.Portals.TryGetValue(portalId, out portal);
    }

    public FieldNpc SpawnNpc(NpcMetadata npc, Vector3 position, Vector3 rotation) {
        int objectId = Interlocked.Increment(ref objectIdCounter);
        var fieldNpc = new FieldNpc(this, objectId, new Npc(npc)) {
            Position = position,
            Rotation = rotation,
        };
        fieldNpcs[objectId] = fieldNpc;

        return fieldNpc;
    }

    public bool TryGetNpc(int objectId, [NotNullWhen(true)] out FieldNpc? npc) {
        return fieldNpcs.TryGetValue(objectId, out npc);
    }

    public void InspectPlayer(GameSession session, long characterId) {
        foreach (FieldPlayer fieldPlayer in fieldPlayers.Values) {
            if (fieldPlayer.Value.Character.Id == characterId) {
                session.Send(PlayerInfoPacket.Load(fieldPlayer.Session));
                return;
            }
        }

        session.Send(PlayerInfoPacket.NotFound(characterId));
    }

    public void Multicast(ByteWriter packet, GameSession? sender = null) {
        foreach ((_, FieldPlayer other) in fieldPlayers) {
            if (other.Session == sender) continue;
            other.Session.Send(packet);
        }
    }

    public void Dispose() { }
}
