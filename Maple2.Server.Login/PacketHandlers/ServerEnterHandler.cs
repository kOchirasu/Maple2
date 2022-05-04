using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Login.PacketHandlers;

public class ServerEnterHandler : PacketHandler<LoginSession> {
    public override ushort OpCode => RecvOp.SERVER_ENTER;

    public ServerEnterHandler(ILogger<ServerEnterHandler> logger) : base(logger) { }

    public override void Handle(LoginSession session, IByteReader packet) {
        packet.Read<Language>();

        session.ListServers();
        session.ListCharacters();
    }
}
