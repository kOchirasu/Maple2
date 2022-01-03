using Maple2.PacketLib.Tools;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Login.PacketHandlers;

public class ResponseKeyHandler : ResponseKeyHandler<LoginSession> {
    public ResponseKeyHandler(ILogger<ResponseKeyHandler> logger) : base(logger) { }

    public override void Handle(LoginSession session, IByteReader packet) {
        long accountId = packet.Peek<long>(); // Peek because base will read this
        // using (UserStorage.Request request = userStorage.Context()) {
        //     session.Account = request.GetAccount(accountId);
        // }

        base.Handle(session, packet);
    }
}
