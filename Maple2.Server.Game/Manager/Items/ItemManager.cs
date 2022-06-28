using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
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

    /// <summary>
    /// Retrieves an gear from inventory or equipment.
    /// </summary>
    /// <param name="uid">Uid of the gear to retrieve</param>
    /// <returns>Item if it exists</returns>
    public Item? GetGear(long uid) {
        Item? item = Inventory.Get(uid, InventoryType.Gear);
        return item ?? Equips.Gear.Values.FirstOrDefault(gear => gear.Uid == uid);
    }

    /// <summary>
    /// Retrieves an outfit from inventory or equipment.
    /// </summary>
    /// <param name="uid">Uid of the outfit to retrieve</param>
    /// <returns>Item if it exists</returns>
    public Item? GetOutfit(long uid) {
        Item? item = Inventory.Get(uid, InventoryType.Outfit);
        return item ?? Equips.Outfit.Values.FirstOrDefault(outfit => outfit.Uid == uid);
    }

    public void Save(GameStorage.Request db) {
        Equips.Save(db);
        Inventory.Save(db);
    }
}
