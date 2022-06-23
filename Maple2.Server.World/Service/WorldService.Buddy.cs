using System.Threading.Tasks;
using Grpc.Core;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Service;

public partial class WorldService {
    public override Task<BuddyResponse> Buddy(BuddyRequest request, ServerCallContext context) {
        if (!playerChannels.TryGetValue(request.ReceiverId, out int channel)) {
            return Task.FromResult(new BuddyResponse());
        }

        if (!channels.TryGetValue(channel, out ChannelClient? channelClient)) {
            return Task.FromResult(new BuddyResponse());
        }

        try {
            return Task.FromResult(channelClient.Buddy(request));
        } catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound) {
            playerChannels.TryRemove(request.ReceiverId, out _);
            return Task.FromResult(new BuddyResponse{Online = false});
        }
    }
}
