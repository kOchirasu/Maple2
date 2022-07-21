using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Server.World.Containers;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Maple2.Server.World.Service;

public partial class WorldService : World.WorldBase {
    private readonly ChannelClientLookup channelClients;
    private readonly ILogger logger = Log.Logger.ForContext<WorldService>();

    public WorldService(IMemoryCache tokenCache, PlayerChannelLookup playerChannels, ChannelClientLookup channelClients) {
        this.tokenCache = tokenCache;
        this.playerChannels = playerChannels;
        this.channelClients = channelClients;
    }

    public override Task<ChannelsResponse> Channels(ChannelsRequest request, ServerCallContext context) {
        var response = new ChannelsResponse();
        response.Channels.AddRange(channelClients.Keys);
        return Task.FromResult(response);
    }

    public override Task<PlayerInfoResponse> PlayerInfo(PlayerInfoRequest request, ServerCallContext context) {
        if (request.AccountId == 0 && request.CharacterId == 0) {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"AccountId and CharacterId not specified"));
        }

        long accountId = request.AccountId;
        long characterId = request.CharacterId;

        int channel;
        if (request.AccountId != 0) {
            playerChannels.LookupAccount(accountId, out characterId, out channel);
        } else {
            playerChannels.LookupCharacter(characterId, out accountId, out channel);
        }

        if (channel != 0 && channelClients.TryGetClient(channel, out Channel.Service.Channel.ChannelClient? client)) {
            try {
                return Task.FromResult(client.PlayerInfo(new PlayerInfoRequest {
                    AccountId = accountId,
                    CharacterId = characterId,
                }));
            } catch { /* Ignored */ }
        }

        return Task.FromResult(new PlayerInfoResponse {
            AccountId = accountId,
            CharacterId = characterId,
            IsOnline = false,
        });
    }
}
