using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Maple2.Server.Channel.Service;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Service;

public partial class ChannelService : Channel.Service.Channel.ChannelBase {
    private readonly GameServer server;
    private readonly ILogger logger;

    public ChannelService(GameServer server, ILogger<ChannelService> logger) {
        this.server = server;
        this.logger = logger;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }
}
