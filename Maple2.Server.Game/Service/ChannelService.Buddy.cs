using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Service;

public partial class ChannelService {
    public override Task<BuddyResponse> Buddy(BuddyRequest request, ServerCallContext context) {
        if (!server.GetSession(request.ReceiverId, out GameSession? session)) {
            throw new RpcException(new Status(StatusCode.NotFound, $"Unable to find: {request.ReceiverId}"));
        }

        return Task.FromResult(new BuddyResponse());
    }
}
