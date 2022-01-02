using Maple2.Server.Core.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Session;

public class GameSession : Core.Network.Session {
    protected override PatchType Type => PatchType.Ignore;

    public GameSession(ILogger<GameSession> logger) : base(logger) { }
}