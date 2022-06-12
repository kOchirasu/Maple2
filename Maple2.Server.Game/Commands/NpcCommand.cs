using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Commands.Common;
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

        AddCommand(new FindCommand<NpcMetadata>(session, npcStorage));

        var id = new Argument<int>("id", "Id of npc to spawn.");

        AddArgument(id);
        this.SetHandler<InvocationContext, int>(Handle, id);
    }

    private void Handle(InvocationContext ctx, int npcId) {
        try {
            if (session.Field == null || !npcStorage.TryGet(npcId, out NpcMetadata? metadata)) {
                ctx.ExitCode = 1;
                return;
            }

            Vector3 position = session.Player.Position;
            Vector3 rotation = session.Player.Rotation;
            session.Field.SpawnNpc(metadata, position, rotation);

            ctx.ExitCode = 0;
        } catch (SystemException ex) {
            ctx.Console.Error.WriteLine(ex.Message);
            ctx.ExitCode = 1;
        }
    }
}
