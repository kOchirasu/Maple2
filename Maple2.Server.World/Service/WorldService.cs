using System;
using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Maple2.Server.Core.Constants;
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
}
