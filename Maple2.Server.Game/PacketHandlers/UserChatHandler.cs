using Grpc.Core;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.World.Service;
using Microsoft.Extensions.Logging;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game.PacketHandlers;

public class UserChatHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.USER_CHAT;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public WorldClient World { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public UserChatHandler(ILogger<UserChatHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        var type = packet.Read<ChatType>();
        string message = packet.ReadUnicodeString();
        string recipient = packet.ReadUnicodeString();
        long clubId = packet.ReadLong();

        switch (type) {
            case ChatType.Normal:
                HandleNormal(session, message);
                break;
            case ChatType.WhisperTo:
                HandleWhisper(session, message, recipient);
                break;
            case ChatType.Party:
                HandleParty(session, message);
                break;
            case ChatType.Guild:
                HandleGuild(session, message);
                break;
            case ChatType.World:
                HandleWorld(session, message);
                break;
            case ChatType.Channel:
                HandleChannel(session, message);
                break;
            case ChatType.Super:
                HandleSuper(session, message);
                break;
            case ChatType.Club:
                HandleClub(session, message, clubId);
                break;
            case ChatType.Wedding:
                HandleWedding(session, message);
                break;
        }
    }

    private static void HandleNormal(GameSession session, string message) {
        session.Field.Multicast(ChatPacket.Message(session.Player.Value, ChatType.Normal, message));
    }

    private void HandleWhisper(GameSession session, string message, string recipient) {
        var request = new ChatRequest {
            Message = message,
            Whisper = new ChatRequest.Types.Whisper {Recipient = recipient},
        };

        try {
            World.Chat(request);
        } catch (RpcException ex) {
            switch (ex.StatusCode) {
                case StatusCode.PermissionDenied:
                    break;
                case StatusCode.Unavailable:
                    break;
            }
        }
    }

    private void HandleParty(GameSession session, string message) {
        var request = new ChatRequest {
            Message = message,
            Party = new ChatRequest.Types.Party {PartyId = 0},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private void HandleGuild(GameSession session, string message) {
        var request = new ChatRequest {
            Message = message,
            Guild = new ChatRequest.Types.Guild {GuildId = 0},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private void HandleWorld(GameSession session, string message) {
        var request = new ChatRequest {
            Message = message,
            World = new ChatRequest.Types.World(),
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private static void HandleChannel(GameSession session, string message) {

    }

    private void HandleSuper(GameSession session, string message) {
        var request = new ChatRequest {
            Message = message,
            Super = new ChatRequest.Types.Super {ItemId = 0},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private void HandleClub(GameSession session, string message, long clubId) {
        var request = new ChatRequest {
            Message = message,
            Club = new ChatRequest.Types.Club {ClubId = clubId},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private void HandleWedding(GameSession session, string message) {
        var request = new ChatRequest {
            Message = message,
            Wedding = new ChatRequest.Types.Wedding {ItemId = 0},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }
}
