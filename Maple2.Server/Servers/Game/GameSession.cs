using Maple2.Server.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Servers.Game {
    public class GameSession : Session {
        protected override PatchType Type => PatchType.Ignore;

        public GameSession(ILogger<GameSession> logger) : base(logger) { }
    }
}