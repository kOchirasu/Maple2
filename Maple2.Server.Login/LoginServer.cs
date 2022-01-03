using Autofac;
using Maple2.Server.Core.Network;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Login;

public class LoginServer : Server<LoginSession> {
    public const int PORT = 21001;

    public LoginServer(PacketRouter<LoginSession> router, ILogger<LoginServer> logger, IComponentContext context)
        : base(PORT, router, logger, context) { }

    protected override void AddSession(LoginSession session) {
        logger.LogInformation("Login client connected: {Session}", session);
        session.Start();
    }
}