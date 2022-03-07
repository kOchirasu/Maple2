using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.World.Service;

public partial class WorldService : World.WorldBase {
    private readonly ILogger logger;

    public WorldService(IMemoryCache tokenCache, ILogger<WorldService> logger) {
        this.tokenCache = tokenCache;
        this.logger = logger;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }
}
