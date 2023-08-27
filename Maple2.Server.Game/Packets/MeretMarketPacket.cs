using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class MeretMarketPacket {
    private enum Command : byte {
        LoadPersonalListings = 11,
        LoadSales = 12,
        ListItem = 13,
        RemoveListing = 14,
        UnlistItem = 15,
        RelistItem = 18,
        CollectProfit = 20,
        UpdateExpiration = 21,
        Unknown22 = 22,
        UpdateProfit = 26, // unconfirmed
        LoadShopCategory = 27,
        Purchase = 30,
        // Initialize = 016,
        Featured = 101,
        OpenDesignShop = 102,
        LoadCart = 107,
        Unknown201 = 201,
    }

    public static ByteWriter LoadItems(IList<MarketItem> marketItems, int totalItems, int startPage) {
        var pWriter = Packet.Of(SendOp.MeretMarket);
        pWriter.Write<Command>(Command.LoadShopCategory);
        pWriter.WriteInt(marketItems.Count);
        pWriter.WriteInt(totalItems);
        pWriter.WriteInt(startPage);
        pWriter.WriteMarketItemEntries(marketItems);

        return pWriter;
    }

    public static ByteWriter Purchase(int totalQuantity, int itemIndex, long price,  int premiumMarketId, long ugcMarketItemId) {
        var pWriter = Packet.Of(SendOp.MeretMarket);
        pWriter.Write<Command>(Command.Purchase);
        pWriter.WriteByte((byte) totalQuantity);
        pWriter.WriteInt(premiumMarketId);
        pWriter.WriteLong(ugcMarketItemId);
        pWriter.WriteInt(1);
        pWriter.WriteInt();
        pWriter.WriteLong();
        pWriter.WriteInt(itemIndex);
        pWriter.WriteInt(totalQuantity);
        pWriter.WriteInt();
        pWriter.WriteByte();
        pWriter.WriteUnicodeString();
        pWriter.WriteUnicodeString();
        pWriter.WriteLong(price);
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteInt();
        return pWriter;
    }
    
    public static ByteWriter Featured(byte section, byte tabId, IList<MarketItem> marketItems) {
        var pWriter = Packet.Of(SendOp.MeretMarket);
        pWriter.Write<Command>(Command.Featured);
        pWriter.WriteByte(section);
        pWriter.WriteByte(tabId);
        pWriter.WriteByte();
        pWriter.WriteByte(3); // this needs to be >= 3? GMS2 has it as 14.
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteMarketItemEntries(marketItems);
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        pWriter.WriteByte();
        return pWriter;
    }

    #region Helpers
    private static void WriteMarketItemEntries(this IByteWriter pWriter, IList<MarketItem> marketItems) {
        foreach (MarketItem entry in marketItems) {
            pWriter.WriteBool(entry is PremiumMarketItem);
            pWriter.WriteBool(entry is UgcMarketItem);
            switch (entry) {
                case PremiumMarketItem premium:
                    pWriter.WriteInt(premium.Id);
                    pWriter.WriteLong();
                    pWriter.WriteClass<PremiumMarketItem>(premium);
                    pWriter.WriteBool(premium.PromoData != null);
                    if (premium.PromoData != null) {
                        pWriter.WriteClass<PremiumMarketPromoData>(premium.PromoData);
                    }
                    pWriter.WriteByte();
                    pWriter.WriteBool(premium.ShowSaleTime);
                    pWriter.WriteInt();
                    pWriter.WriteByte((byte) premium.AdditionalQuantities.Count);
                    foreach (PremiumMarketItem addEntry in premium.AdditionalQuantities) {
                        pWriter.WriteBool(true);
                        pWriter.WriteClass<PremiumMarketItem>(addEntry);
                        bool unk = false;
                        pWriter.WriteBool(unk);
                        if (unk) {
                            pWriter.WriteString();
                            pWriter.WriteLong();
                            pWriter.WriteUnicodeString();
                            pWriter.WriteLong();
                        }
                        pWriter.WriteByte();
                        pWriter.WriteByte();
                    }
                    break;
                case UgcMarketItem ugc:
                    pWriter.WriteClass<UgcMarketItem>(ugc);
                    break;
            }
        }
    }
    #endregion
}
