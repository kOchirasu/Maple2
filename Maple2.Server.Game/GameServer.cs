using System.Collections.Generic;
using Autofac;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game;

public class GameServer : Server<GameSession> {
    private readonly List<GameSession> sessions;

    public GameServer(PacketRouter<GameSession> router, ILogger<GameServer> logger, IComponentContext context)
        : base(Target.GAME_PORT, router, logger, context) {
        this.sessions = new List<GameSession>();
    }

    public IEnumerable<GameSession> GetSessions() {
        sessions.RemoveAll(session => !session.Connected());
        return sessions;
    }

    protected override void AddSession(GameSession session) {
        sessions.Add(session);
        logger.LogInformation("Game client connected: {Session}", session);
        session.Start();
    }
}
