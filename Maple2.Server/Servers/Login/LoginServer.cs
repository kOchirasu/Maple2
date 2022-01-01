using Autofac;
using Maple2.Server.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Servers.Login;

public class LoginServer : Server<LoginSession> {
    public const int PORT = 20001;

    public LoginServer(PacketRouter<LoginSession> router, ILogger<LoginServer> logger, IComponentContext context)
        : base(router, logger, context) { }

    public void Start() {
        base.Start(PORT);
    }

    public override void AddSession(LoginSession session) {
        logger.LogInformation("Login client connected: {Session}", session);
        session.Start();
    }
}