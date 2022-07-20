using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Server.Game.Session;
using Serilog;

namespace Maple2.Server.Game.Service;

public partial class ChannelService : Channel.Service.Channel.ChannelBase {
    private readonly GameServer server;
    private readonly ILogger logger = Log.Logger.ForContext<ChannelService>();

    public ChannelService(GameServer server) {
        this.server = server;
    }

    public override Task<PlayerInfoResponse> PlayerInfo(PlayerInfoRequest request, ServerCallContext context) {
        if (!server.GetSession(request.CharacterId, out GameSession? session) || !session.Connected()) {
            return Task.FromResult(new PlayerInfoResponse {
                AccountId = request.AccountId,
                CharacterId = request.CharacterId,
                IsOnline = false,
                Channel = 0,
            });
        }

        return Task.FromResult(new PlayerInfoResponse {
            AccountId = request.AccountId,
            CharacterId = request.CharacterId,
            IsOnline = true,
            Channel = server.Channel,
        });
    }
}
