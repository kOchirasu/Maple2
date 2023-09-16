﻿using System.Linq;
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
        TalkAlliance = 9,
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
                HandleQuest(session, packet);
                return;
            case Command.AcceptAllianceQuest:
                HandleAcceptAllianceQuest(session, packet);
                return;
            case Command.TalkAlliance:
                HandleTalkAlliance(session);
                return;
            case Command.Custom:
                return;
        }
    }

    private void HandleClose(GameSession session) {
        session.NpcScript = null;
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
            session.Shop.Load(npc.Value.Metadata.Basic.ShopId, npc.Value.Id);
        }

        if (!ScriptMetadata.TryGet(npc.Value.Id, out ScriptMetadata? metadata)) {
            session.Send(NpcTalkPacket.Respond(npc, NpcTalkType.None, default));
            return;
        }

        var scriptContext = new NpcScriptContext(session);
        session.NpcScript = scriptLoader.GetNpc(session, scriptContext, npc, metadata);
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

        session.NpcScript.ExitState();
        int pick = packet.ReadInt();

        /* The ordering is
        / Quests
        / Dialog
        / Talk */
        int addedOptions = 0;
        if (session.NpcScript.TalkType.HasFlag(NpcTalkType.Select)) {
            if (session.NpcScript.TalkType.HasFlag(NpcTalkType.Quest)) {
                addedOptions += session.NpcScript.Quests.Count;
                // Picked quest
                if (pick < addedOptions) {
                    FieldNpc npc = session.NpcScript.Npc;
                    if (!session.ScriptMetadata.TryGet(session.NpcScript.Quests.ElementAt(pick).Value.Id, out ScriptMetadata? metadata)) {
                        session.Send(NpcTalkPacket.Respond(npc, NpcTalkType.None, default));
                        session.NpcScript = null;
                        return;
                    }
                    var scriptContext = new NpcScriptContext(session);
                    session.NpcScript = scriptLoader.GetQuest(session, scriptContext, npc, metadata);
                    if (session.NpcScript == null) {
                        session.Send(NpcTalkPacket.Respond(npc, NpcTalkType.None, default));
                        return;
                    }

                    if (!session.NpcScript.BeginQuest()) {
                        session.NpcScript = null;
                    }
                    return;
                }
            }

            NpcDialogue dialogue;
            if (session.NpcScript.TalkType.HasFlag(NpcTalkType.Dialog)) {
                addedOptions++;
                if (pick < addedOptions) {
                    session.NpcScript.EnterDialog();
                    dialogue = new NpcDialogue(session.NpcScript.State, session.NpcScript.Index, session.NpcScript.Button);
                    session.Send(NpcTalkPacket.Continue(session.NpcScript.TalkType, dialogue));
                    return;
                }
            }

            session.NpcScript.EnterTalk();
            dialogue = new NpcDialogue(session.NpcScript.State, session.NpcScript.Index, session.NpcScript.Button);
            session.Send(NpcTalkPacket.Continue(session.NpcScript.TalkType, dialogue));
            return;
        }


        // Attempt to Continue, if |false|, the dialogue has terminated.
        if (!session.NpcScript.Continue(pick)) {
            session.NpcScript = null;
        }
    }

    private void HandleQuest(GameSession session, IByteReader packet) {
        if (session.NpcScript == null) {
            return;
        }

        int questId = packet.ReadInt();
        packet.ReadShort(); // 2 or 0. 2 = Start quest, 0 = Complete quest.

        FieldNpc npc = session.NpcScript.Npc;
        if (!session.ScriptMetadata.TryGet(questId, out ScriptMetadata? metadata)) {
            session.Send(NpcTalkPacket.Respond(npc, NpcTalkType.None, default));
            session.NpcScript = null;
            return;
        }
        var scriptContext = new NpcScriptContext(session);
        session.NpcScript = scriptLoader.GetQuest(session, scriptContext, npc, metadata);
        if (session.NpcScript == null) {
            session.Send(NpcTalkPacket.Respond(npc, NpcTalkType.None, default));
            return;
        }

        if (!session.NpcScript.BeginQuest()) {
            session.NpcScript = null;
        }
    }

    private void HandleAcceptAllianceQuest(GameSession session, IByteReader packet) {
        if (session.NpcScript == null) {
            return;
        }

        int questId = packet.ReadInt();
        packet.ReadShort(); // 2 or 0. 2 = Start quest, 0 = Complete quest.

        // TODO: similar to HandleQuest but we'll need to check questId against the available quests for the player.
    }

    private void HandleTalkAlliance(GameSession session) {
        if (session.NpcScript == null) {
            return;
        }
        session.NpcScript.EnterTalk();
        var dialogue = new NpcDialogue(session.NpcScript.State, session.NpcScript.Index, session.NpcScript.Button);
        session.Send(NpcTalkPacket.AllianceTalk(session.NpcScript.TalkType, dialogue));
    }
}
