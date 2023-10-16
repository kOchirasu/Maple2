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
        if (!(request.GroupChatId != 0 && partyLookup.TryGet(request.GroupChatId, out GroupChatManager? manager))) {
            return Task.FromResult(new GroupChatInfoResponse());
        }

        return Task.FromResult(new GroupChatInfoResponse { GroupChat = ToGroupChatInfo(manager.GroupChat) });
    }

    public override Task<PartyResponse> GroupChat(GroupChatRequest request, ServerCallContext context) {
        switch (request.GroupChatCase) {
            case GroupChatRequest.GroupChatOneofCase.Create:
                return Task.FromResult(CreateGroupChat(request.RequestorId, request.Create));
            case GroupChatRequest.GroupChatOneofCase.Invite:
                return Task.FromResult(DisbandParty(request.RequestorId, request.Disband));
            case GroupChatRequest.GroupChatOneofCase.Leave:
                return Task.FromResult(InviteParty(request.RequestorId, request.Invite));
            default:
                return Task.FromResult(new GroupChatResponse { Error = (int) GroupChatError.none });
        }
    }

    private PartyResponse CreateGroupChat(long requestorId, GroupChatRequest.Types.Create create) {
        GroupChatError error = partyLookup.Create(requestorId, out int partyId);
        if (error != PartyError.none) {
            return new PartyResponse { Error = (int) error };
        }
        if (!partyLookup.TryGet(partyId, out PartyManager? manager)) {
            return new PartyResponse { Error = (int) PartyError.s_party_err_not_found };
        }

        return new PartyResponse { Party = ToPartyInfo(manager.Party) };
    }

    private PartyResponse InviteParty(long requestorId, PartyRequest.Types.Invite invite) {
        if (!partyLookup.TryGet(invite.PartyId, out PartyManager? manager)) {
            return new PartyResponse { Error = (int) PartyError.s_party_err_not_found };
        }
        if (!playerLookup.TryGet(invite.ReceiverId, out PlayerInfo? info)) {
            return new PartyResponse { Error = (int) PartyError.s_party_err_cannot_invite };
        }

        PartyError error = manager.Invite(requestorId, info);
        if (error != PartyError.none) {
            return new PartyResponse { Error = (int) error };
        }

        return new PartyResponse { PartyId = invite.PartyId };
    }

    private PartyResponse RespondInviteParty(long characterId, PartyRequest.Types.RespondInvite respond) {
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

    private PartyResponse LeaveParty(long requestorId, PartyRequest.Types.Leave leave) {
        if (!partyLookup.TryGet(leave.PartyId, out PartyManager? manager)) {
            return new PartyResponse { Error = (int) PartyError.s_party_err_not_found };
        }

        PartyError error = manager.Leave(requestorId);
        if (error != PartyError.none) {
            return new PartyResponse { Error = (int) error };
        }

        return new PartyResponse { PartyId = leave.PartyId };
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
