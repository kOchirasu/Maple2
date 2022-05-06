using Maple2.Database.Storage;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Items;

public class ItemManager {
    private readonly GameSession session;

    public readonly EquipManager Equips;
    public readonly InventoryManager Inventory;

    public ItemManager(GameStorage.Request db, GameSession session) {
        this.session = session;

        Equips = new EquipManager(db, session);
        Inventory = new InventoryManager(db, session);
    }

    public void Save(GameStorage.Request db) {
        Equips.Save(db);
        Inventory.Save(db);
    }
}
