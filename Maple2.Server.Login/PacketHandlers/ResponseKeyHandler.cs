using Maple2.PacketLib.Tools;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.Logging;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Login.PacketHandlers;

public class ResponseKeyHandler : ResponseKeyHandler<LoginSession> {
    public ResponseKeyHandler(WorldClient worldClient, ILogger<ResponseKeyHandler> logger) : base(worldClient, logger) { }

    public override void Handle(LoginSession session, IByteReader packet) {
        // using (UserStorage.Request request = userStorage.Context()) {
        //     session.Account = request.GetAccount(session.accountId);
        // }

        base.Handle(session, packet);
    }
}
