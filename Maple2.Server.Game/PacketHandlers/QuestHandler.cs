using System;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class QuestHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Quest;

    private enum Command : byte {
        Accept = 2,
        Complete = 4,
        Abandon = 6,
        Exploration = 8,
        Tracking = 9,
        GoToNpc = 12,
        GoToDungeon = 13,
        SkyFortress = 14,
        MapleGuide = 16,
        ResumeDungeon = 19,
        Dispatch = 20,
        RemoteComplete = 24, // Maybe? This is mainly used for Navigator
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required ItemMetadataStorage ItemMetadata { private get; init; }
    public required TableMetadataStorage TableMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Accept:
                HandleAccept(session, packet);
                break;
            case Command.Complete:
                HandleComplete(session, packet);
                break;
            case Command.Abandon:
                HandleForfeit(session, packet);
                break;
            case Command.Tracking:
                HandleSetTracking(session, packet);
                break;
            case Command.GoToNpc:
                HandleGoToNpc(session, packet);
                break;
            case Command.RemoteComplete:
                HandleRemoteComplete(session, packet);
                return;
        }
    }
    
    private void HandleAccept(GameSession session, IByteReader packet) {
        int questId = packet.ReadInt();
        int npcObjectId = packet.ReadInt();
        
        if (session.Field == null || !session.Field.Npcs.TryGetValue(npcObjectId, out FieldNpc? npc)) {
            return; // Invalid Npc
        }

        session.Quest.Start(questId);
    }
    
    private void HandleComplete(GameSession session, IByteReader packet) {
        int questId = packet.ReadInt();
        
        if (!session.Quest.TryGetQuest(questId, out Quest? quest)) {
            return;
        }
        if (!session.Quest.Complete(quest)) {
            Logger.Warning("Could not complete quest {QuestId}", questId);
        }
    }
    
    private void HandleForfeit(GameSession session, IByteReader packet) {
        int questId = packet.ReadInt();
        
        if (!session.Quest.TryGetQuest(questId, out Quest? quest) || !quest.Metadata.Basic.Forfeitable) {
            return;
        }

        if (quest.CompletionCount > 0) {
            quest.State = QuestState.Completed; // ?? how do you revert?
        }

        if (session.Quest.Remove(quest)) {
            session.Send(QuestPacket.Abandon(quest.Id));
        }
    }

    private void HandleSetTracking(GameSession session, IByteReader packet) {
        int questId = packet.ReadInt();
        bool tracking = packet.ReadBool();
        
        if (!session.Quest.TryGetQuest(questId, out Quest? quest) || quest.State is QuestState.Completed) {
            return;
        }
        
        session.Send(QuestPacket.SetTracking(questId, tracking));
    }

    private void HandleGoToNpc(GameSession session, IByteReader packet) {
        int questId = packet.ReadInt();

        if (!session.Quest.TryGetQuest(questId, out Quest? quest) || !quest.Metadata.GoToNpc.Enabled) {
            return;
        }
        
        session.Send(session.PrepareField(quest.Metadata.GoToNpc.MapId, portalId: quest.Metadata.GoToNpc.PortalId)
            ? FieldEnterPacket.Request(session.Player)
            : FieldEnterPacket.Error(MigrationError.s_move_err_default));
    }

    private void HandleRemoteComplete(GameSession session, IByteReader packet) {
        int questId = packet.ReadInt();

        if (!session.Quest.TryGetQuest(questId, out Quest? quest)) {
            return;
        }

        session.Quest.Complete(quest);
    }
}
