using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Shop;

public class CharacterShopItemData {
    public readonly int ShopId;
    public readonly int ShopItemId;
    public readonly long CharacterId;
    public readonly long AccountId;
    public readonly long ItemUid;
    public int StockPurchased { get; init; }
    public readonly bool IsPersistant;
    
    public CharacterShopItemData(int shopId, int shopItemId, long itemUid, long characterId, long accountId, bool isPersistant) {
        ShopId = shopId;
        ShopItemId = shopItemId;
        ItemUid = itemUid;
        CharacterId = characterId;
        AccountId = accountId;
        IsPersistant = isPersistant;
    }
}
