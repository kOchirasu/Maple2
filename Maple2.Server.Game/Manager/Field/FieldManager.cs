using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Field;

public sealed partial class FieldManager : IDisposable {
    public readonly MapMetadata Metadata;
    private readonly MapEntityMetadata entities;

    private readonly ConcurrentDictionary<int, FieldEntity<Portal>> fieldPortals = new();

    public int MapId => Metadata.Id;
    public readonly int InstanceId;

    private FieldManager(int instanceId, MapMetadata metadata, MapEntityMetadata entities) {
        InstanceId = instanceId;
        this.Metadata = metadata;
        this.entities = entities;

        foreach (Portal portal in entities.Portals.Values) {
            int objectId = Interlocked.Increment(ref objectIdCounter);
            var fieldPortal = new FieldEntity<Portal>(objectId, portal) {
                Position = portal.Position,
                Rotation = portal.Rotation
            };
            fieldPortals[objectId] = fieldPortal;
        }
    }

    public bool TryGetPortal(int portalId, [NotNullWhen(true)] out Portal? portal) {
        return entities.Portals.TryGetValue(portalId, out portal);
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
