using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Script.Npc;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Scripting;
using Maple2.Server.Game.Scripting.Npc;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class NpcTalkHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.NPC_TALK;

    private enum Command : byte {
        Close = 0,
        Talk = 1,
        Select = 2,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public NpcMetadataStorage NpcMetadata { get; init; } = null!;
    // ReSharper restore All
    #endregion

    private readonly NpcScriptLoader scriptLoader;

    public NpcTalkHandler(ILogger<NpcTalkHandler> logger) : base(logger) {
        scriptLoader = new NpcScriptLoader();
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Close:
                HandleClose(session);
                return;
            case Command.Talk:
                HandleTalk(session, packet);
                return;
            case Command.Select:
                HandleSelect(session, packet);
                return;
        }
    }

    private void HandleClose(GameSession session) {
        session.NpcScript?.Close();
        session.NpcScript = null;
    }

    private void HandleTalk(GameSession session, IByteReader packet) {
        // Already talking to an Npc.
        if (session.NpcScript != null) {
            return;
        }

        int objectId = packet.ReadInt();
        if (session.Field == null || !session.Field.TryGetNpc(objectId, out FieldNpc? npc)) {
            return; // Invalid Npc
        }

        if (!NpcMetadata.TryGet(npc.Value.Id, out NpcMetadata? metadata)) {
            return;
        }

        session.NpcScript = scriptLoader.Get(npc.Value.Id, new NpcScriptContext(session, npc));
        if (session.NpcScript == null) {
            return;
        }

        session.NpcScript.Talk();
    }

    private void HandleSelect(GameSession session, IByteReader packet) {
        // Not talking to an Npc.
        if (session.NpcScript == null) {
            return;
        }

        int selection = packet.ReadInt();
        session.NpcScript.Advance(selection);
        session.NpcScript.Select();
    }
}
