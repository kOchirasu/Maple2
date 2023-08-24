using System.Collections.Generic;
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
        Home = 101,
        OpenDesignShop = 102,
        LoadCart = 107,
        Unknown201 = 201,
    }

    public static ByteWriter LoadShopCategory(ICollection<MarketEntry> marketEntries) {
        var pWriter = Packet.Of(SendOp.MeretMarket);
        pWriter.Write<Command>(Command.LoadShopCategory);
        pWriter.WriteInt(marketEntries.Count);
        pWriter.WriteInt(marketEntries.Count); // total,not just this page
        pWriter.WriteInt(1); // needed?
        
        // loop start..ish
        foreach (MarketEntry entry in marketEntries) {
            pWriter.WriteBool(true);
            pWriter.WriteBool(entry is UgcMarketEntry);
            switch (entry) {
                case PremiumMarketEntry premium:
                    pWriter.WriteInt(premium.Id);
                    pWriter.WriteLong();
                    pWriter.WriteClass<PremiumMarketEntry>(premium);
                    pWriter.WriteBool(premium.PromoData != null);
                    if (premium.PromoData != null) {
                        pWriter.WriteClass<PremiumMarketPromoData>(premium.PromoData);
                    }
                    pWriter.WriteByte();
                    pWriter.WriteBool(premium.ShowSaleTime);
                    pWriter.WriteInt();
                    pWriter.WriteByte((byte) premium.AdditionalQuantities.Count);
                    foreach(PremiumMarketEntry addEntry in premium.AdditionalQuantities) {
                        pWriter.WriteBool(true);
                        pWriter.WriteClass<PremiumMarketEntry>(addEntry);
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
                case UgcMarketEntry ugc:
                    pWriter.WriteClass<UgcMarketEntry>(ugc);
                    break;
            }
        }
            

        return pWriter;
    }

    public static ByteWriter Purchase() {
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
}
