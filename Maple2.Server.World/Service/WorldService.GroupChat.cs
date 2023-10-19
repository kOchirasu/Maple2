using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.GroupChat;
using Maple2.Model.Game.Party;
using Maple2.Server.World.Containers;
using ChannelPartyRequest = Maple2.Server.Channel.Service.PartyRequest;

namespace Maple2.Server.World.Service;

public partial class WorldService {
    public override Task<GroupChatInfoResponse> GroupChatInfo(GroupChatInfoRequest request, ServerCallContext context) {
        if (!(request.GroupChatId != 0 && groupChatLookup.TryGet(request.GroupChatId, out GroupChatManager? manager))) {
            return Task.FromResult(new GroupChatInfoResponse());
        }

        return Task.FromResult(new GroupChatInfoResponse { GroupChat = ToGroupChatInfo(manager.GroupChat) });
    }

    public override Task<GroupChatResponse> GroupChat(GroupChatRequest request, ServerCallContext context) {
        switch (request.GroupChatCase) {
            case GroupChatRequest.GroupChatOneofCase.Create:
                return Task.FromResult(CreateGroupChat(request.RequestorId, request.Create));
            case GroupChatRequest.GroupChatOneofCase.Invite:
                return Task.FromResult(InviteGroupChat(request.RequestorId, request.Invite));
            case GroupChatRequest.GroupChatOneofCase.Leave:
                return Task.FromResult(LeaveGroupChat(request.RequestorId, request.Leave));
            default:
                return Task.FromResult(new GroupChatResponse { Error = (int) GroupChatError.none });
        }
    }

    private GroupChatResponse CreateGroupChat(long requesterId, GroupChatRequest.Types.Create create) {
        GroupChatError error = groupChatLookup.Create(requesterId, out int partyId);
        if (error != GroupChatError.none) {
            return new GroupChatResponse { Error = (int) error };
        }
        if (!groupChatLookup.TryGet(partyId, out GroupChatManager? manager)) {
            return new GroupChatResponse { Error = (int) GroupChatError.s_err_groupchat_null_target_user };
        }

        return new GroupChatResponse { GroupChat = ToGroupChatInfo(manager.GroupChat) };
    }

    private GroupChatResponse InviteGroupChat(long requestorId, GroupChatRequest.Types.Invite invite) {
        if (!groupChatLookup.TryGet(invite.GroupChatId, out GroupChatManager? manager)) {
            return new GroupChatResponse { Error = (int) GroupChatError.s_err_groupchat_null_target_user };
        }
        if (!playerLookup.TryGet(invite.ReceiverId, out PlayerInfo? info)) {
            return new GroupChatResponse { Error = (int) GroupChatError.s_err_groupchat_null_target_user };
        }

        GroupChatError error = manager.Invite(requestorId, info);
        /*if (error != PartyError.none) {
            return new PartyResponse { Error = (int) error };
        }*/

        return new GroupChatResponse { GroupChatId = invite.GroupChatId };
    }

    private PartyResponse RespondInviteGroupChat(long characterId, PartyRequest.Types.RespondInvite respond) {
        if (!partyLookup.TryGet(respond.PartyId, out PartyManager? manager)) {
            return new PartyResponse { Error = (int) PartyError.s_party_err_not_found };
        }
        string requestorName = manager.ConsumeInvite(characterId);
        if (string.IsNullOrEmpty(requestorName)) {
            return new PartyResponse { Error = (int) PartyError.s_party_err_cannot_invite };
        }
        if (!playerLookup.TryGet(characterId, out PlayerInfo? info)) {
            return new PartyResponse { Error = (int) PartyError.none };
        }

        if (respond.Reply == (int) PartyInvite.Response.Accept) {
            PartyError error = manager.Join(info);
            if (error != PartyError.none) {
                return new PartyResponse { Error = (int) error };
            }

            return new PartyResponse { Party = ToPartyInfo(manager.Party) };
        }

        manager.Broadcast(new ChannelPartyRequest {
            InviteReply = new ChannelPartyRequest.Types.InviteReply {
                Name = info.Name,
                Reply = respond.Reply,
            },
        });

        return new PartyResponse { PartyId = manager.Party.Id };
    }

    private GroupChatResponse LeaveGroupChat(long requestorId, GroupChatRequest.Types.Leave leave) {
        if (!groupChatLookup.TryGet(leave.GroupChatId, out GroupChatManager? manager)) {
            return new GroupChatResponse { Error = (int) GroupChatError.s_err_groupchat_null_target_user };
        }

        return new GroupChatResponse { GroupChatId = leave.GroupChatId };
    }

    private static GroupChatInfo ToGroupChatInfo(GroupChat groupChat) {
        return new GroupChatInfo {
            Id = groupChat.Id,
            Members = {
                groupChat.Members.Values.Select(member => new GroupChatInfo.Types.Member {
                    CharacterId = member.CharacterId,
                }),
            },
        };
    }
}
