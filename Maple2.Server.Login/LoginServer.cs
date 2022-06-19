using Autofac;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Login.Session;

namespace Maple2.Server.Login;

public class LoginServer : Server<LoginSession> {
    public LoginServer(PacketRouter<LoginSession> router, IComponentContext context)
        : base(Target.LOGIN_PORT, router, context) { }

    protected override void AddSession(LoginSession session) {
        Logger.Information("Login client connected: {Session}", session);
        session.Start();
    }
}
