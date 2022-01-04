using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Maple2.Server.Channel.Service;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Service;

public class ChannelService : Channel.Service.Channel.ChannelBase {
    private readonly ILogger logger;

    public ChannelService(ILogger<ChannelService> logger) {
        this.logger = logger;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }
}
