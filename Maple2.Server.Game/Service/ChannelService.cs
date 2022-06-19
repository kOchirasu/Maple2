using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Maple2.Server.Channel.Service;
using Serilog;

namespace Maple2.Server.Game.Service;

public partial class ChannelService : Channel.Service.Channel.ChannelBase {
    private readonly GameServer server;
    private readonly ILogger logger = Log.Logger.ForContext<ChannelService>();

    public ChannelService(GameServer server) {
        this.server = server;
    }

    public override Task<HealthResponse> Health(Empty request, ServerCallContext context) {
        return Task.FromResult(new HealthResponse());
    }
}
