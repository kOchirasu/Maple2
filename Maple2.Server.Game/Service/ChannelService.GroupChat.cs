using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.GroupChat;
using Maple2.Model.Game.Party;
using Maple2.Server.Game.Manager;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.World.Service;
using GroupChatRequest = Maple2.Server.Channel.Service.GroupChatRequest;
using GroupChatResponse = Maple2.Server.Channel.Service.GroupChatResponse;

namespace Maple2.Server.Game.Service;

public partial class ChannelService {
    public override Task<GroupChatResponse> GroupChat(GroupChatRequest request, ServerCallContext context) {
        switch (request.GroupChatCase) {
            case GroupChatRequest.GroupChatOneofCase.Invite:
                return Task.FromResult(GroupChatInvite(request.GroupChatId, request.ReceiverIds, request.Invite));
            case GroupChatRequest.GroupChatOneofCase.AddMember:
                return Task.FromResult(AddGroupChatMember(request.GroupChatId, request.ReceiverIds, request.AddMember));
            case GroupChatRequest.GroupChatOneofCase.RemoveMember:
                return Task.FromResult(RemoveGroupChatMember(request.GroupChatId, request.ReceiverIds, request.RemoveMember));
            default:
                return Task.FromResult(new GroupChatResponse { Error = (int) GroupChatError.s_err_groupchat_null_target_user });
        }
    }

    private GroupChatResponse GroupChatInvite(int groupChatId, IEnumerable<long> receiverIds, GroupChatRequest.Types.Invite invite) {
        foreach (long receiverId in receiverIds) {
            if (!server.GetSession(receiverId, out GameSession? session)) {
                return new GroupChatResponse { Error = (int) PartyError.s_party_err_alreadyInvite };
            }

            // Check if the receiver is already in a party, and if it has more than 1 member
            if (session.Party.Party != null && session.Party.Party.Members.Count > 1) {
                if (session.Party.Party.LeaderCharacterId == receiverId) {
                    session.Send(PartyPacket.JoinRequest(invite.SenderName));
                    return new GroupChatResponse { Error = (int) PartyError.s_party_request_invite };
                }
                return new GroupChatResponse { Error = (int) PartyError.s_party_err_cannot_invite };
            }
            // Remove any existing 1 person party
            session.Party.RemoveParty();

            session.Send(PartyPacket.Invite(groupChatId, invite.SenderName));
        }

        return new GroupChatResponse();
    }

    private GroupChatResponse AddGroupChatMember(int groupChatId, IEnumerable<long> receiverIds, GroupChatRequest.Types.AddMember add) {
        if (!playerInfos.GetOrFetch(add.CharacterId, out PlayerInfo? info)) {
            return new GroupChatResponse { Error = (int) PartyError.s_party_err_cannot_invite };
        }

        foreach (long characterId in receiverIds) {
            if (!server.GetSession(characterId, out GameSession? session)) {
                continue;
            }

            if (!session.GroupChats.TryGetValue(groupChatId, out GroupChatManager? manager)) {
                continue;
            }
            GroupChatMember? sender = manager.GetMember(characterId);
            if (sender == null) {
                continue;
            }
            manager.AddMember(sender, new GroupChatMember {
                Info = info.Clone(),
            });
        }

        return new GroupChatResponse();
    }

    private GroupChatResponse RemoveGroupChatMember(int groupChatId, IEnumerable<long> receiverIds, GroupChatRequest.Types.RemoveMember remove) {
        foreach (long characterId in receiverIds) {
            if (!server.GetSession(characterId, out GameSession? session)) {
                continue;
            }

            if (!session.GroupChats.TryGetValue(groupChatId, out GroupChatManager? manager)) {
                continue;
            }

            bool isSelf = characterId == remove.CharacterId;
            manager.RemoveMember(remove.CharacterId, isSelf);
            if (isSelf) {
                session.Party.RemoveParty();
            }
        }

        return new GroupChatResponse();
    }
}

