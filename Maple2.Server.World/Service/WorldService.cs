using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Service;

public partial class WorldService : World.WorldBase {
    private readonly List<ChannelClient> channels;
    private readonly ILogger logger = Log.Logger.ForContext<WorldService>();

    public WorldService(IEnumerable<ChannelClient> channels, IMemoryCache tokenCache) {
        this.channels = channels.ToList();
        this.tokenCache = tokenCache;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }
}
