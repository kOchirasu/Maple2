using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Shop;

public class CharacterShopData {
    public required int ShopId { get; init; }
    public long RestockTime { get; set; }
    public int RestockCount { get; set; }
    public ShopRestockInterval Interval { get; init; }
}
