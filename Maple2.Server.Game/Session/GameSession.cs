using Maple2.Model.Game;
using Maple2.Server.Core.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Session;

public class GameSession : Core.Network.Session {
    protected override PatchType Type => PatchType.Ignore;

    public Account Account;
    public Character Character;

    public GameSession(ILogger<GameSession> logger) : base(logger) { }
}