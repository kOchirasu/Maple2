using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Service;

public partial class WorldService {
    public override Task<BuddyResponse> Buddy(BuddyRequest request, ServerCallContext context) {
        if (!playerChannels.Lookup(request.ReceiverId, out int channel)) {
            return Task.FromResult(new BuddyResponse{Online = false});
        }

        if (!channelClients.TryGetClient(channel, out ChannelClient? channelClient)) {
            logger.Error("No registry for channel: {Channel}", channel);
            return Task.FromResult(new BuddyResponse());
        }

        try {
            return Task.FromResult(channelClient.Buddy(request));
        } catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound) {
            playerChannels.Remove(request.ReceiverId);
            logger.Information("{CharacterId} not found...", request.ReceiverId);
            return Task.FromResult(new BuddyResponse{Online = false});
        }
    }
}
