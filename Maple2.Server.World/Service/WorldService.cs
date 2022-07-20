using System;
using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Maple2.Database.Storage;
using Maple2.Server.Core.Constants;
using Maple2.Server.World.Containers;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Maple2.Server.World.Service;

public partial class WorldService : World.WorldBase {
    private readonly ChannelClientLookup channelClients;
    private readonly GameStorage gameStorage;
    private readonly ILogger logger = Log.Logger.ForContext<WorldService>();

    public WorldService(IMemoryCache tokenCache, PlayerChannelLookup playerChannels, ChannelClientLookup channelClients, GameStorage gameStorage) {
        this.tokenCache = tokenCache;
        this.playerChannels = playerChannels;
        this.channelClients = channelClients;
        this.gameStorage = gameStorage;
    }

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context) {
        lock (channelClients) {
            // Prevent concurrent requests from registering the same channel.
            int channel = 1;
            if (request.HasChannel) {
                if (channelClients.Contains(request.Channel)) {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Requested channel is already registered: {request.Channel}"));
                }

                channel = request.Channel;
            } else {
                // Find the first valid channel
                while (channelClients.Contains(channel)) {
                    channel++;
                }
            }

            var gameEndpoint = new IPEndPoint(IPAddress.Parse(request.IpAddress), request.Port);
            string channelService = Environment.GetEnvironmentVariable("CHANNEL_SERVICE") ?? IPAddress.Loopback.ToString();
            var grpcEndpoint = new IPEndPoint(IPAddress.Parse(channelService), Target.GrpcChannelPort);
            GrpcChannel grpcChannel = GrpcChannel.ForAddress($"http://{grpcEndpoint}");
            if (!channelClients.TryAdd(channel, gameEndpoint, grpcChannel)) {
                throw new RpcException(new Status(StatusCode.Internal, $"Failed to add registered game channel: {channel}"));
            }

            return Task.FromResult(new RegisterResponse {Channel = channel});
        }
    }

    public override Task<UnregisterResponse> Unregister(UnregisterRequest request, ServerCallContext context) {
        channelClients.Remove(request.Channel);
        return Task.FromResult(new UnregisterResponse());
    }

    public override Task<ChannelsResponse> Channels(ChannelsRequest request, ServerCallContext context) {
        // ReSharper disable once InconsistentlySynchronizedField
        return Task.FromResult(new ChannelsResponse {ChannelCount = channelClients.Count});
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
            return Task.FromResult(client.PlayerInfo(new PlayerInfoRequest {
                AccountId = accountId,
                CharacterId = characterId,
            }));
        }

        return Task.FromResult(new PlayerInfoResponse {
            AccountId = accountId,
            CharacterId = characterId,
            IsOnline = false,
        });
    }
}
