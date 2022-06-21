using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Maple2.Server.Core.Constants;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Service;

public partial class WorldService : World.WorldBase {
    private readonly IReadOnlyDictionary<int, ChannelClient> channels;
    private readonly ILogger logger = Log.Logger.ForContext<WorldService>();

    public WorldService(GrpcClientFactory factory, IMemoryCache tokenCache) {
        var builder = ImmutableDictionary.CreateBuilder<int, ChannelClient>();
        builder[Target.GAME_CHANNEL] = factory.CreateClient<ChannelClient>(Target.GAME_CHANNEL.ToString());

        this.channels = builder.ToImmutable();
        this.tokenCache = tokenCache;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }
}
