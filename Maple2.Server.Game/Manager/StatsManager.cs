using Maple2.Server.Game.Model;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager;

public class StatsManager {
    private readonly GameSession session;

    public Stats Values = new Stats();

    public StatsManager(GameSession session) {
        this.session = session;
    }
}
