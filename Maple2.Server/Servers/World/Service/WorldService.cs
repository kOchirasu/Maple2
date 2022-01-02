using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Servers.World.Service;

public class WorldService : Greeter.GreeterBase {
    private readonly ILogger logger;

    public WorldService(ILogger<WorldService> logger) {
        this.logger = logger;
    }

    // Server side handler of the SayHello RPC
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context) {
        return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
    }

    // Server side handler for the SayHelloAgain RPC
    public override Task<HelloReply> SayHelloAgain(HelloRequest request, ServerCallContext context) {
        return Task.FromResult(new HelloReply { Message = "Hello again " + request.Name });
    }
}
