using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands;

public class NpcCommand : Command {
    private const string NAME = "npc";
    private const string DESCRIPTION = "Npc spawning.";

    private readonly GameSession session;
    private readonly NpcMetadataStorage npcStorage;

    public NpcCommand(GameSession session, NpcMetadataStorage npcStorage) : base(NAME, DESCRIPTION) {
        this.session = session;
        this.npcStorage = npcStorage;

        var id = new Argument<int>("id", "Id of npc to spawn.");

        AddArgument(id);
        this.SetHandler<InvocationContext, int>(Handle, id);
    }

    private void Handle(InvocationContext ctx, int npcId) {
        try {
            if (session.Field == null || !npcStorage.TryGet(npcId, out NpcMetadata? metadata)) {
                ctx.Console.Error.WriteLine($"Invalid Npc: {npcId}");
                return;
            }

            Vector3 position = session.Player.Position;
            Vector3 rotation = session.Player.Rotation;
            FieldNpc fieldNpc = session.Field.SpawnNpc(metadata, position, rotation);
            session.Field.Multicast(FieldPacket.AddNpc(fieldNpc));
            session.Field.Multicast(ProxyObjectPacket.AddNpc(fieldNpc));
            fieldNpc.Sync();

            ctx.ExitCode = 0;
        } catch (SystemException ex) {
            ctx.Console.Error.WriteLine(ex.Message);
            ctx.ExitCode = 1;
        }
    }
}
