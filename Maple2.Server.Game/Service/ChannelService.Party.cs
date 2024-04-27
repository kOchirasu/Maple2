﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.Party;
using Maple2.Server.Channel.Service;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Service;

public partial class ChannelService {
    public override Task<PartyResponse> Party(PartyRequest request, ServerCallContext context) {
        switch (request.PartyCase) {
            case PartyRequest.PartyOneofCase.Invite:
                return Task.FromResult(PartyInvite(request.PartyId, request.ReceiverIds, request.Invite));
            case PartyRequest.PartyOneofCase.InviteReply:
                return Task.FromResult(PartyInviteReply(request.ReceiverIds, request.InviteReply));
            case PartyRequest.PartyOneofCase.AddMember:
                return Task.FromResult(AddPartyMember(request.PartyId, request.ReceiverIds, request.AddMember));
            case PartyRequest.PartyOneofCase.RemoveMember:
                return Task.FromResult(RemovePartyMember(request.ReceiverIds, request.RemoveMember));
            case PartyRequest.PartyOneofCase.UpdateLeader:
                return Task.FromResult(UpdatePartyLeader(request.ReceiverIds, request.UpdateLeader));
            case PartyRequest.PartyOneofCase.Disband:
                return Task.FromResult(Disband(request.ReceiverIds, request.Disband));
            default:
                return Task.FromResult(new PartyResponse { Error = (int) PartyError.s_party_err_not_found });
        }
    }

    private PartyResponse PartyInvite(int partyId, IEnumerable<long> receiverIds, PartyRequest.Types.Invite invite) {
        foreach (long receiverId in receiverIds) {
            if (!server.GetSession(receiverId, out GameSession? session)) {
                return new PartyResponse { Error = (int) PartyError.s_party_err_alreadyInvite };
            }
            if (session.Buddy.IsBlocked(invite.SenderId)) {
                return new PartyResponse { Error = (int) PartyError.s_party_err_cannot_invite };
            }
            // Check if the receiver is already in a party, and if it has more than 1 member
            if (session.Party.Party != null && session.Party.Party.Members.Count > 1) {
                if (session.Party.Party.LeaderCharacterId == receiverId) {
                    session.Send(PartyPacket.JoinRequest(invite.SenderName));
                    return new PartyResponse { Error = (int) PartyError.s_party_request_invite };
                }
                return new PartyResponse { Error = (int) PartyError.s_party_err_cannot_invite };
            }
            // Remove any existing 1 person party
            session.Party.RemoveParty();

            session.Send(PartyPacket.Invite(partyId, invite.SenderName));
        }

        return new PartyResponse();
    }

    private PartyResponse PartyInviteReply(IEnumerable<long> receiverIds, PartyRequest.Types.InviteReply reply) {
        foreach (long characterId in receiverIds) {
            if (!server.GetSession(characterId, out GameSession? session)) {
                continue;
            }

            session.Send(PartyPacket.Error((PartyError) reply.Reply, reply.Name));
        }

        return new PartyResponse();
    }

    private PartyResponse AddPartyMember(long partyId, IEnumerable<long> receiverIds, PartyRequest.Types.AddMember add) {
        if (!playerInfos.GetOrFetch(add.CharacterId, out PlayerInfo? info)) {
            return new PartyResponse { Error = (int) PartyError.s_party_err_cannot_invite };
        }

        foreach (long characterId in receiverIds) {
            if (!server.GetSession(characterId, out GameSession? session)) {
                continue;
            }

            session.Party.AddMember(new PartyMember {
                PartyId = partyId,
                Info = info.Clone(),
                JoinTime = add.JoinTime,
                LoginTime = add.LoginTime,
            });
        }

        return new PartyResponse();
    }

    private PartyResponse RemovePartyMember(IEnumerable<long> receiverIds, PartyRequest.Types.RemoveMember remove) {
        foreach (long characterId in receiverIds) {
            if (!server.GetSession(characterId, out GameSession? session)) {
                continue;
            }

            bool isSelf = characterId == remove.CharacterId;
            session.Party.RemoveMember(remove.CharacterId, remove.IsKicked, isSelf);
            if (isSelf) {
                session.Party.RemoveParty();
            }
        }

        return new PartyResponse();
    }

    private PartyResponse UpdatePartyLeader(IEnumerable<long> receiverIds, PartyRequest.Types.UpdateLeader update) {
        foreach (long characterId in receiverIds) {
            if (!server.GetSession(characterId, out GameSession? session)) {
                continue;
            }

            session.Party.UpdateLeader(update.CharacterId);
        }

        return new PartyResponse();
    }

    private PartyResponse Disband(IEnumerable<long> receiverIds, object disband) {
        foreach (long characterId in receiverIds) {
            if (!server.GetSession(characterId, out GameSession? session)) {
                continue;
            }

            session.Party.Disband();
        }

        return new PartyResponse();
    }
}

