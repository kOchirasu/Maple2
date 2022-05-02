using System;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Field;

public sealed partial class FieldManager : IDisposable {
    private readonly MapMetadata metadata;
    private readonly MapEntityMetadata entities;

    public int MapId => metadata.Id;
    public readonly int InstanceId;

    private FieldManager(int instanceId, MapMetadata metadata, MapEntityMetadata entities) {
        InstanceId = instanceId;
        this.metadata = metadata;
        this.entities = entities;
    }

    public void Multicast(ByteWriter packet, GameSession sender = null) {
        foreach ((_, FieldPlayer other) in fieldPlayers) {
            if (other.Session == sender) continue;
            other.Session.Send(packet);
        }
    }

    public void Dispose() { }
}
