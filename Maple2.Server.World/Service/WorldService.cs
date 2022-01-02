using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.World.Service;

public class WorldService : World.WorldBase {
    private readonly ILogger logger;

    public WorldService(ILogger<WorldService> logger) {
        this.logger = logger;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse { Ok = true });
    }

    // Server side handler of the SayHello RPC
    public override Task<HelloResponse> SayHello(HelloRequest request, ServerCallContext context) {
        return Task.FromResult(new HelloResponse { Message = "Hello " + request.Name });
    }
}