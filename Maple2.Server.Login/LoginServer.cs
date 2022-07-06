using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Maple2.Model.Game;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.Login.Session;

namespace Maple2.Server.Login;

public class LoginServer : Server<LoginSession> {
    private readonly object mutex = new();
    private readonly HashSet<LoginSession> connectingSessions;
    private readonly Dictionary<long, LoginSession> sessions;

    public LoginServer(PacketRouter<LoginSession> router, IComponentContext context)
            : base(Target.LOGIN_PORT, router, context) {
        connectingSessions = new HashSet<LoginSession>();
        sessions = new Dictionary<long, LoginSession>();
    }

    public override void OnConnected(LoginSession session) {
        lock (mutex) {
            connectingSessions.Remove(session);
            sessions[session.AccountId] = session;
        }
    }

    public override void OnDisconnected(LoginSession session) {
        lock (mutex) {
            connectingSessions.Remove(session);
            sessions.Remove(session.AccountId);
        }
    }

    public bool GetSession(long accountId, [NotNullWhen(true)] out LoginSession? session) {
        lock (mutex) {
            return sessions.TryGetValue(accountId, out session);
        }
    }

    protected override void AddSession(LoginSession session) {
        lock (mutex) {
            connectingSessions.Add(session);
        }

        Logger.Information("Login client connected: {Session}", session);
        session.Start();
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        lock (mutex) {
            foreach (LoginSession session in connectingSessions) {
                session.Send(NoticePacket.Disconnect(new InterfaceText("LoginServer Maintenance")));
                session.Dispose();
            }
            foreach (LoginSession session in sessions.Values) {
                session.Send(NoticePacket.Disconnect(new InterfaceText("LoginServer Maintenance")));
                session.Dispose();
            }
        }

        return base.StopAsync(cancellationToken);
    }
}
