using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Shop;

public class ShopItem : IByteSerializable {
    public int Id;
    public int ItemId { get; init; }
    public ShopCost Cost { get; init; } = ShopCost.Zero;
    public byte Rarity { get; init; }
    public int StockCount { get; init; }
    public int StockPurchased { get; init; }
    public string Category { get; init; }
    public int RequireGuildTrophy { get; init; }
    public int RequireAchievementId { get; init; }
    public int RequireAchievementRank { get; init; }
    public byte RequireChampionshipGrade { get; init; }
    public short RequireChampionshipJoinCount { get; init; }
    public byte RequireGuildMerchantType { get; init; }
    public short RequireGuildMerchantLevel { get; init; }
    public short Quantity { get; init; }
    public ShopItemLabel Label { get; init; }
    public string CurrencyIdString { get; init; }
    public short RequireQuestAllianceId { get; init; }
    public int RequireFameGrade { get; init; }
    public bool AutoPreviewEquip { get; init; }
    public Item? Item;
    
    public ShopItem(int id) {
        Id = id;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteInt(ItemId);
        writer.WriteClass<ShopCost>(Cost);
        writer.WriteByte(Rarity);
        writer.WriteInt();
        writer.WriteInt(StockCount);
        writer.WriteInt(StockPurchased * Quantity);
        writer.WriteInt(RequireGuildTrophy);
        writer.WriteString(Category);
        writer.WriteInt(RequireAchievementId);
        writer.WriteInt(RequireAchievementRank);
        writer.WriteByte(RequireChampionshipGrade);
        writer.WriteShort(RequireChampionshipJoinCount);
        writer.WriteByte(RequireGuildMerchantType);
        writer.WriteShort(RequireGuildMerchantLevel);
        writer.WriteBool(false);
        writer.WriteShort(Quantity);
        writer.WriteByte();
        writer.Write<ShopItemLabel>(Label);
        writer.WriteString(CurrencyIdString);
        writer.WriteShort(RequireQuestAllianceId);
        writer.WriteInt(RequireFameGrade);
        writer.WriteBool(AutoPreviewEquip);
        
        //TODO: Implement buy period
        bool buyPeriod = false;
        writer.WriteBool(buyPeriod);
        if (buyPeriod) {
            bool timeSpecific = true;
            writer.WriteBool(timeSpecific);
            writer.WriteLong(); // start time
            writer.WriteLong(); // end time
            writer.WriteBool(true); // unknown
            writer.WriteByte(1); // amount of buy periods count.
            
            // loop start
            writer.WriteInt(); // time begin in seconds. ex 1200 = 12:20 AM
            writer.WriteInt(); // time end in seconds. ex 10600 = 2:56 AM
            // loop end
            
            writer.WriteByte(1); // days of the week you can buy at. loop
            // loop start
            writer.WriteByte(); // 1 = Sunday, 7 = Saturday
            // loop end
        }
        writer.WriteClass<Item>(Item);
    }
}
