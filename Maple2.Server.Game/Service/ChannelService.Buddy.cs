using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Service;

public partial class ChannelService {
    public override Task<BuddyResponse> Buddy(BuddyRequest request, ServerCallContext context) {
        if (!server.GetSession(request.ReceiverId, out GameSession? session)) {
            throw new RpcException(new Status(StatusCode.NotFound, $"Unable to find: {request.ReceiverId}"));
        }

        switch (request.BuddyCase) {
            case BuddyRequest.BuddyOneofCase.Invite:
                session.Buddy.ReceiveInvite(request.Invite.SenderId);
                break;
            case BuddyRequest.BuddyOneofCase.Accept:
                session.Buddy.ReceiveAccept(request.Accept.EntryId);
                break;
            case BuddyRequest.BuddyOneofCase.Decline:
                session.Buddy.ReceiveDecline(request.Decline.EntryId);
                break;
            case BuddyRequest.BuddyOneofCase.Block:
                break;
            case BuddyRequest.BuddyOneofCase.Unblock:
                break;
            case BuddyRequest.BuddyOneofCase.Cancel:
                break;
        }

        return Task.FromResult(new BuddyResponse{Online = true});
    }
}
