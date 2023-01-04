using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Scripting.Npc;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class NpcTalkHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.NpcTalk;

    private enum Command : byte {
        Close = 0,
        Talk = 1,
        Continue = 2,
        EnchantUnknown = 4,
        Enchant = 6,
        Quest = 7,
        AcceptAllianceQuest = 8,
        TalkAllianceQuest = 9,
        Custom = 11,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required NpcMetadataStorage NpcMetadata { private get; init; }
    public required ScriptMetadataStorage ScriptMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    private readonly NpcScriptLoader scriptLoader;

    public NpcTalkHandler() {
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
            case Command.Continue:
                HandleContinue(session, packet);
                return;
            case Command.EnchantUnknown:
                return;
            case Command.Enchant:
                return;
            case Command.Quest:
                return;
            case Command.AcceptAllianceQuest:
                return;
            case Command.TalkAllianceQuest:
                return;
            case Command.Custom:
                return;
        }
    }

    private void HandleClose(GameSession session) {
        if (session.NpcScript == null) {
            return;
        }

        session.NpcScript = null;
        session.Send(NpcTalkPacket.Close());
    }

    private void HandleTalk(GameSession session, IByteReader packet) {
        // Already talking to an Npc.
        if (session.NpcScript != null) {
            return;
        }

        int objectId = packet.ReadInt();
        if (session.Field == null || !session.Field.Npcs.TryGetValue(objectId, out FieldNpc? npc)) {
            return; // Invalid Npc
        }

        if (npc.Value.Metadata.Basic.ShopId > 0) {
            // TODO: Load NPC Shop
            Logger.Warning("Shop {Id} not loaded", npc.Value.Metadata.Basic.ShopId);
        }

        if (!ScriptMetadata.TryGet(npc.Value.Id, out ScriptMetadata? metadata)) {
            session.Send(NpcTalkPacket.Respond(npc, NpcTalkType.None, default));
            return;
        }

        var scriptContext = new NpcScriptContext(session, npc, metadata);
        session.NpcScript = scriptLoader.Get(npc.Value.Id, scriptContext);
        if (session.NpcScript == null) {
            session.Send(NpcTalkPacket.Respond(npc, NpcTalkType.None, default));
            return;
        }

        // If we fail to begin the interaction, set script to null.
        if (!session.NpcScript.Begin()) {
            session.NpcScript = null;
        }
    }

    private void HandleContinue(GameSession session, IByteReader packet) {
        // Not talking to an Npc.
        if (session.NpcScript == null) {
            return;
        }

        int pick = packet.ReadInt();
        // Attempt to Continue, if |false|, the dialogue has terminated.
        if (!session.NpcScript.Continue(pick)) {
            session.NpcScript = null;
        }
    }
}
