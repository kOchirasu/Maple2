using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game;

public class GameServer : Server<GameSession> {
    private readonly object mutex = new object();
    private readonly HashSet<GameSession> connectingSessions;
    private readonly Dictionary<long, GameSession> sessions;

    public GameServer(PacketRouter<GameSession> router, ILogger<GameServer> logger, IComponentContext context)
            : base(Target.GAME_PORT, router, logger, context) {
        connectingSessions = new HashSet<GameSession>();
        sessions = new Dictionary<long, GameSession>();
    }

    public void OnConnected(GameSession session) {
        lock (mutex) {
            connectingSessions.Remove(session);
            sessions[session.CharacterId] = session;
        }
    }

    public void OnDisconnected(GameSession session) {
        lock (mutex) {
            connectingSessions.Remove(session);
            sessions.Remove(session.CharacterId);
        }
    }

    public bool GetSession(long characterId, out GameSession? session) {
        return sessions.TryGetValue(characterId, out session);
    }

    protected override void AddSession(GameSession session) {
        lock (mutex) {
            connectingSessions.Add(session);
        }

        logger.LogInformation("Game client connecting: {Session}", session);
        session.Start();
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        lock (mutex) {
            foreach (GameSession session in connectingSessions) {
                session.Dispose();
            }
            foreach (GameSession session in sessions.Values) {
                session.Dispose();
            }
        }

        return base.StopAsync(cancellationToken);
    }
}
