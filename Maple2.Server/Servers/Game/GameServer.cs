using System.Collections.Generic;
using Autofac;
using Maple2.Server.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Servers.Game;

public class GameServer : Server<GameSession> {
    public const int PORT = 21001;

    private readonly List<GameSession> sessions;

    public GameServer(PacketRouter<GameSession> router, ILogger<GameServer> logger, IComponentContext context)
        : base(router, logger, context) {
        this.sessions = new List<GameSession>();
    }

    public void Start() {
        base.Start(PORT);
    }

    public IEnumerable<GameSession> GetSessions() {
        sessions.RemoveAll(session => !session.Connected());
        return sessions;
    }

    public override void AddSession(GameSession session) {
        sessions.Add(session);
        logger.LogInformation($"Game client connected: {session}");
        session.Start();
    }
}