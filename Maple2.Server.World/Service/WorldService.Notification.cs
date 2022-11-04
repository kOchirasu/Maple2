using System.Threading.Tasks;
using Grpc.Core;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Service;

public partial class WorldService {
    public override Task<MailNotificationResponse> MailNotification(MailNotificationRequest request, ServerCallContext context) {
        if (!playerChannels.LookupCharacter(request.CharacterId, out _, out int channel)) {
            return Task.FromResult(new MailNotificationResponse());
        }

        if (!channelClients.TryGetClient(channel, out ChannelClient? channelClient)) {
            logger.Error("No registry for channel: {Channel}", channel);
            return Task.FromResult(new MailNotificationResponse());
        }

        try {
            return Task.FromResult(channelClient.MailNotification(request));
        } catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound) {
            playerChannels.RemoveByCharacter(request.CharacterId);
            logger.Information("{CharacterId} not found...", request.CharacterId);
            return Task.FromResult(new MailNotificationResponse());
        }
    }
}
