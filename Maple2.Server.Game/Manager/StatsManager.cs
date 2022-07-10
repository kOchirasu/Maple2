using Maple2.Model;
using Maple2.Model.Game;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager;

public class StatsManager {
    private readonly GameSession session;

    public readonly Stats Values;

    public StatsManager(GameSession session) {
        this.session = session;

        Player player = session.Player;
        Values = new Stats(player.Character.Job.Code(), player.Character.Level);
    }

    public void Refresh() {
        Character character = session.Player.Value.Character;
        Values.Reset(character.Job.Code(), character.Level);
        // TODO: after resetting, we also need to re-add stats from buffs/equips.

        session.Send(StatsPacket.Init(session.Player));
        session.Field?.Broadcast(StatsPacket.Update(session.Player), session);
    }
}
