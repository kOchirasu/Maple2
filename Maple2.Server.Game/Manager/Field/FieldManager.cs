using System;
using System.Collections.Concurrent;
using System.Threading;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Field;

public sealed partial class FieldManager : IDisposable {
    private readonly MapMetadata metadata;
    private readonly MapEntityMetadata entities;

    private readonly ConcurrentDictionary<int, FieldEntity<Portal>> fieldPortals = new();

    public int MapId => metadata.Id;
    public readonly int InstanceId;

    private FieldManager(int instanceId, MapMetadata metadata, MapEntityMetadata entities) {
        InstanceId = instanceId;
        this.metadata = metadata;
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

    public void Multicast(ByteWriter packet, GameSession? sender = null) {
        foreach ((_, FieldPlayer other) in fieldPlayers) {
            if (other.Session == sender) continue;
            other.Session.Send(packet);
        }
    }

    public void Dispose() { }
}
