using Maple2.Database.Storage;
using Maple2.Model.Enum;
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
        Next = 2,
        EnchantUnknown = 4,
        Enchant = 6,
        Quest = 7,
        AcceptAllianceQuest = 8,
        TalkAllianceQuest = 9,
        Custom = 11,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public NpcMetadataStorage NpcMetadata { get; init; } = null!;
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
            case Command.Next:
                HandleNext(session, packet);
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

        if (!NpcMetadata.TryGet(npc.Value.Id, out NpcMetadata? metadata)) {
            return;
        }

        var scriptContext = new NpcScriptContext(session, npc);
        int kind = metadata.Basic.Kind;
        switch (kind) {
            case 1 or (> 10 and < 20):
                scriptContext.Respond(NpcTalkType.Dialog, 1, NpcTalkButton.None);
                // TODO: Load Shop
                return;
            case 2:
                scriptContext.Respond(NpcTalkType.Dialog, 1, NpcTalkButton.None);
                // TODO: Storage
                return;
            case 86:
                break; // TODO: BlackMarket
            case 88:
                break; // TODO: Birthday
            case >= 100 and <= 104:
                break; // TODO: Sky Fortress
            case >= 105 and <= 107:
                break; // TODO: Kritias
        }

        session.NpcScript = scriptLoader.Get(npc.Value.Id, scriptContext);
        if (session.NpcScript == null) {
            session.Send(NpcTalkPacket.Close());
            return;
        }

        session.NpcScript.Respond();
    }

    private void HandleNext(GameSession session, IByteReader packet) {
        // Not talking to an Npc.
        if (session.NpcScript == null) {
            return;
        }

        int selection = packet.ReadInt();
        session.NpcScript.Advance(selection);

        // Attempt to Continue, if |false|, the dialogue has terminated.
        if (!session.NpcScript.Continue()) {
            session.NpcScript = null;
        }
    }
}
