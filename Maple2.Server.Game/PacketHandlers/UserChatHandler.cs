﻿using Grpc.Core;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
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
    public GameStorage GameStorage { private get; init; } = null!;
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
                return;
            case ChatType.WhisperTo:
                HandleWhisper(session, message, recipient);
                return;
            case ChatType.Party:
                HandleParty(session, message);
                return;
            case ChatType.Guild:
                HandleGuild(session, message);
                return;
            case ChatType.World:
                HandleWorld(session, message);
                return;
            case ChatType.Channel:
                HandleChannel(session, message);
                return;
            case ChatType.Super:
                HandleSuper(session, message);
                return;
            case ChatType.Club:
                HandleClub(session, message, clubId);
                return;
            case ChatType.Wedding:
                HandleWedding(session, message);
                return;
        }
    }

    private static void HandleNormal(GameSession session, string message) {
        session.Field.Multicast(ChatPacket.Message(session.Player.Value, ChatType.Normal, message));
    }

    private void HandleWhisper(GameSession session, string message, string recipient) {
        if (recipient == session.Player.Value.Character.Name) {
            session.Send(ChatPacket.Alert(StringCode.s_whisper_err_myself));
            return;
        }

        using GameStorage.Request db = GameStorage.Context();
        CharacterInfo? info = db.GetCharacterInfo(recipient);
        if (info == null) {
            session.Send(ChatPacket.Alert(StringCode.s_whisper_err_target));
            return;
        }

        var request = new ChatRequest {
            CharacterId = session.CharacterId,
            Name = session.Player.Value.Character.Name,
            Message = message,
            Whisper = new ChatRequest.Types.Whisper {RecipientId = info.CharacterId, RecipientName = recipient},
        };

        try {
            World.Chat(request);
            session.Send(ChatPacket.Whisper(info.AccountId, info.CharacterId, info.Name, request.Message));
        } catch (RpcException ex) {
            switch (ex.StatusCode) {
                case StatusCode.NotFound:
                    session.Send(ChatPacket.Alert(StringCode.s_err_cannot_find_user));
                    return;
                case StatusCode.PermissionDenied:
                    session.Send(ChatPacket.WhisperReject(recipient));
                    return;
                case StatusCode.Unavailable:
                    return;
            }
        }
    }

    private void HandleParty(GameSession session, string message) {
        var request = new ChatRequest {
            CharacterId = session.CharacterId,
            Name = session.Player.Value.Character.Name,
            Message = message,
            Party = new ChatRequest.Types.Party {PartyId = 0},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private void HandleGuild(GameSession session, string message) {
        var request = new ChatRequest {
            CharacterId = session.CharacterId,
            Name = session.Player.Value.Character.Name,
            Message = message,
            Guild = new ChatRequest.Types.Guild {GuildId = 0},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private void HandleWorld(GameSession session, string message) {
        var request = new ChatRequest {
            CharacterId = session.CharacterId,
            Name = session.Player.Value.Character.Name,
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
            CharacterId = session.CharacterId,
            Name = session.Player.Value.Character.Name,
            Message = message,
            Super = new ChatRequest.Types.Super {ItemId = 0},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private void HandleClub(GameSession session, string message, long clubId) {
        var request = new ChatRequest {
            CharacterId = session.CharacterId,
            Name = session.Player.Value.Character.Name,
            Message = message,
            Club = new ChatRequest.Types.Club {ClubId = clubId},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }

    private void HandleWedding(GameSession session, string message) {
        var request = new ChatRequest {
            CharacterId = session.CharacterId,
            Name = session.Player.Value.Character.Name,
            Message = message,
            Wedding = new ChatRequest.Types.Wedding {ItemId = 0},
        };

        try {
            World.Chat(request);
        } catch (RpcException) { }
    }
}