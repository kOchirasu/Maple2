using System;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Channel.Service;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class GroupChatHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.GroupChat;

    private enum Command : byte {
        Create = 1,
        Invite = 2,
        Leave = 4,
        Chat = 10,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required TableMetadataStorage TableMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Create:
                HandleCreate(session);
                break;
            case Command.Invite:
                HandleInvite(session, packet);
                break;
            case Command.Leave:
                HandleLeave(session, packet);
                break;
            case Command.Chat:
                HandleChat(session, packet);
                break;
        }
    }

    private void HandleCreate(GameSession session) {
        GroupChatResponse  response = World.GroupChat(new GroupChatRequest {

        })
    }

    private void HandleInvite(GameSession session, IByteReader packet) {
        string targetPlayerName = packet.ReadUnicodeString();
        int groupChatId = packet.ReadInt();
    }

    private void HandleLeave(GameSession session, IByteReader packet) {
        int groupChatId = packet.ReadInt();
    }

    private void HandleChat(GameSession session, IByteReader packet) {
        string message = packet.ReadUnicodeString();
        int groupChatId = packet.ReadInt();
    }
}
