using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Shop;

public class CharacterShopItemData {
    public int ShopId { get; init; }
    public int ShopItemId { get; init; }
    public int StockPurchased { get; set; }
}
