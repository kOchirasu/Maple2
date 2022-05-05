using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Service;

public partial class WorldService : World.WorldBase {
    private readonly List<ChannelClient> channels;
    private readonly ILogger logger;

    public WorldService(IEnumerable<ChannelClient> channels, IMemoryCache tokenCache, ILogger<WorldService> logger) {
        this.channels = channels.ToList();
        this.tokenCache = tokenCache;
        this.logger = logger;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }
}
