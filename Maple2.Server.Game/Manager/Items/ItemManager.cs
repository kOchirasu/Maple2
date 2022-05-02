using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Items;

public class ItemManager {
    private readonly GameSession session;

    public readonly EquipManager Equips;

    public ItemManager(GameSession session) {
        this.session = session;

        Equips = new EquipManager(session);
    }
}
