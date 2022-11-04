using System.Threading.Tasks;
using Grpc.Core;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Service;

public partial class ChannelService {
    public override Task<MailNotificationResponse> MailNotification(MailNotificationRequest request, ServerCallContext context) {
        if (!server.GetSession(request.CharacterId, out GameSession? session)) {
            throw new RpcException(new Status(StatusCode.NotFound, $"Unable to find: {request.CharacterId}"));
        }

        session.Mail.Notify(true);

        return Task.FromResult(new MailNotificationResponse{Delivered = true});
    }
}
