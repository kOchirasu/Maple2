using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Community.CsharpSqlite;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.Event;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class MeretMarketHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.MeretMarket;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required ItemMetadataStorage ItemMetadata { private get; init; }
    public required TableMetadataStorage TableMetadata { private get; init; }

    // ReSharper restore All
    #endregion

    private enum Command : byte {
        LoadPersonalListings = 11,
        LoadSales = 12,
        ListItem = 13,
        RemoveListing = 14,
        UnlistItem = 15,
        RelistItem = 18,
        CollectProfit = 20,
        LoadBookmarks = 22,
        AddBookmark = 23,
        RemoveBookmark = 25,
        OpenShop = 27,
        FindItem = 29,
        Purchase = 30,
        Featured = 101,
        OpenDesignShop = 102,
        Search = 104,
        LoadCart = 107, // Just a guess?
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.OpenShop:
                HandleOpenShop(session, packet);
                return;
            case Command.Purchase:
                HandlePurchase(session, packet);
                return;
            case Command.Featured:
                HandleFeatured(session, packet);
                return;
            case Command.FindItem:
                HandleFindItem(session, packet);
                return;
            case Command.Search:
                HandleSearch(session, packet);
                return;
        }
    }

    private void HandleOpenShop(GameSession session, IByteReader packet) {
        int tabId = packet.ReadInt();
        var gender = packet.Read<GenderFlag>();
        var job = packet.Read<JobFlag>();
        var sort = packet.Read<MeretMarketSort>();
        string searchString = packet.ReadUnicodeString();
        int startPage = packet.ReadInt();
        int unknown = packet.ReadInt(); // repeats the startPage value? Possibly for pagination. Needs confirmation
        MeretMarketSection section = GetMarketSection(packet.ReadByte());
        packet.ReadByte();
        byte itemsPerPage = packet.ReadByte();

        ICollection<MarketItem> entries = Filter(session, section, tabId, gender, job, searchString);
        // use this to concat ugc items
        /*IList<MarketItem> entriess = entries.ToList();
        entries = entriess.Concat(entries).ToList();*/
        int totalItems = entries.Count;
        entries = TakeLimit(entries, startPage, itemsPerPage);
        entries = Sort(entries, sort);

        session.Send(MeretMarketPacket.LoadItems(entries.ToList(), totalItems, startPage));
    }

    private static void HandlePurchase(GameSession session, IByteReader packet) {
        byte quantity = packet.ReadByte();
        int premiumMarketId = packet.ReadInt();
        long ugcItemId = packet.ReadLong();
        if (ugcItemId > 0) {
            // TODO: Handle UGC item purchasing
            return;
        }

        packet.ReadInt();
        int childMarketItemId = packet.ReadInt();
        long unk1 = packet.ReadLong();
        int itemIndex = packet.ReadInt();
        int totalQuantity = packet.ReadInt();
        int unk2 = packet.ReadInt();
        byte unk3 = packet.ReadByte();
        string unk4 = packet.ReadUnicodeString();
        string unk5 = packet.ReadUnicodeString();
        long price = packet.ReadLong();

        using GameStorage.Request db = session.GameStorage.Context();
        PremiumMarketItem? entry = db.GetPremiumMarketEntry(childMarketItemId == 0 ? premiumMarketId : childMarketItemId);
        if (entry == null) {
            return;
        }

        price = entry.SalePrice > 0 ? entry.SalePrice : entry.Price;
        StringCode payResult = Pay(session, price, entry.CurrencyType);
        if (payResult != StringCode.s_empty_string) {
            session.Send(NoticePacket.MessageBox(payResult));
            return;
        }

        Item? newItem = session.Item.CreateItem(entry.ItemId, entry.Rarity, entry.Quantity + entry.BonusQuantity);
        if (newItem == null) {
            return;
        }

        if (entry.ItemDuration > 0) {
            newItem.ExpiryTime = (long) (DateTime.Now.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds + entry.ItemDuration;
        }

        if (!session.Item.Inventory.Add(newItem, true)) {
            session.Item.MailItem(newItem);
        }

        session.Send(MeretMarketPacket.Purchase(totalQuantity, itemIndex, price, premiumMarketId, ugcItemId));
    }

    private static void HandleFeatured(GameSession session, IByteReader packet) {
        byte section = packet.ReadByte();
        byte tabId = packet.ReadByte();

        ICollection<MarketItem> entries = new List<MarketItem>();
        using GameStorage.Request db = session.GameStorage.Context();
        switch (GetMarketSection(section)) {
            case MeretMarketSection.All:
                entries = db.GetMarketItems(MeretMarketSection.All, tabId);
                break;
            case MeretMarketSection.Premium:
            case MeretMarketSection.Ugc:
            case MeretMarketSection.RedMeret:
                break;
        }

        session.Send(MeretMarketPacket.Featured(section, tabId, entries.ToList()));
    }

    private void HandleFindItem(GameSession session, IByteReader packet) {
        bool premium = packet.ReadBool();
        MarketItem? marketItem;
        using GameStorage.Request db = session.GameStorage.Context();
        if (premium) {
            int premiumId = packet.ReadInt();
            marketItem = db.GetPremiumMarketEntry(premiumId);
            if (marketItem == null) {
                return;
            }
        } else {
            long ugcId = packet.ReadLong();
            // TODO: Implement UGC Items
            return;
        }
        
        session.Send(MeretMarketPacket.LoadItems(new List<MarketItem> { marketItem }, 1, 1));
    }

    private void HandleSearch(GameSession session, IByteReader packet) {
        packet.ReadInt(); // 1
        var gender = packet.Read<GenderFlag>();
        var job = packet.Read<JobFlag>();
        var sort = packet.Read<MeretMarketSort>();
        string searchString = packet.ReadUnicodeString();
        int startPage = packet.ReadInt(); // 1
        packet.ReadInt(); // 1
        packet.ReadByte();
        packet.ReadByte();
        byte itemsPerPage = packet.ReadByte();
        MeretMarketSection section = GetMarketSection(packet.ReadByte());

        /*string testName = "Crazy Dance Emote";
        bool containsE = testName.Contains("e", StringComparison.OrdinalIgnoreCase);
        return;*/
        
        ICollection<MarketItem> entries = Filter(session, section, 0, gender, job, searchString);
        // use this to concat ugc items
        /*IList<MarketItem> entriess = entries.ToList();
        entries = entriess.Concat(entries).ToList();*/
        int totalItems = entries.Count;
        entries = TakeLimit(entries, startPage, itemsPerPage);
        entries = Sort(entries, sort);
        
        session.Send(MeretMarketPacket.LoadItems(entries.ToList(), totalItems, startPage));
    }
    
    private static MeretMarketSection GetMarketSection(byte section) {
        return section switch {
            0 => MeretMarketSection.All,
            1 => MeretMarketSection.Premium,
            2 => MeretMarketSection.RedMeret,
            3 => MeretMarketSection.Ugc,
            _ => MeretMarketSection.All,
        };
    }

    private ICollection<MarketItem> Filter(GameSession session, MeretMarketSection section, int tabId, GenderFlag gender, JobFlag job, string searchString) {
        using GameStorage.Request db = session.GameStorage.Context();
        if (tabId == 0) {
            return db.GetPremiumMarketEntries(false, false, gender, job, searchString);
        }
        if (!TableMetadata.MeretMarketCategoryTable.Entries.TryGetValue((int) section, out IReadOnlyDictionary<int, MeretMarketCategoryTable.Tab>? sectionDictionary) ||
            !sectionDictionary.TryGetValue(tabId, out MeretMarketCategoryTable.Tab? tab)) {
            return new List<MarketItem>();
        }

        // get any sub tabs
        int[] tabIds = new[] {
            tabId
        }.Concat(tab.SubTabIds).ToArray();

        return db.GetPremiumMarketEntries(tab.SortGender, tab.SortJob, gender, job, searchString, tabIds);
    }

    private static StringCode Pay(GameSession session, long price, MeretMarketCurrencyType currencyType) {
        switch (currencyType) {
            case MeretMarketCurrencyType.Meso:
                if (session.Currency.CanAddMeso(-price) != -price) {
                    return StringCode.s_err_lack_meso;
                }
                session.Currency.Meso -= price;
                return StringCode.s_empty_string;
            case MeretMarketCurrencyType.Meret:
                if (session.Currency.CanAddMeret(-price) != -price) {
                    return StringCode.s_err_lack_merat;
                }
                session.Currency.Meret -= price;
                return StringCode.s_empty_string;
            case MeretMarketCurrencyType.RedMeret: // TODO: Implement Red Meret
                return StringCode.s_err_lack_merat_red;
            default:
                return StringCode.s_err_lack_money;
        }
    }

    private static ICollection<MarketItem> Sort(IEnumerable<MarketItem> entries, MeretMarketSort sort) {
        switch (sort) {
            case MeretMarketSort.MostRecent:
                return entries.OrderByDescending(entry => entry.CreationTime).ToList();
            case MeretMarketSort.PriceHighest:
                return entries.OrderByDescending(entry => entry.Price).ToList();
            case MeretMarketSort.PriceLowest:
                return entries.OrderBy(entry => entry.Price).ToList();
            // TODO: Implement most popular?
            // Unsure how most popular is different than top seller.
            case MeretMarketSort.MostPopularPremium:
            case MeretMarketSort.MostPopularUgc:
            case MeretMarketSort.TopSeller:
                return entries.OrderByDescending(entry => entry.SalesCount).ToList();
            case MeretMarketSort.None:
            default:
                return entries.ToList();
        }
    }

    /// <summary>
    /// Limits the amount of market items returned to the client.
    /// </summary>
    /// <returns>Limited Market Items. 5 * itemsPerPage</returns>
    private static ICollection<MarketItem> TakeLimit(IEnumerable<MarketItem> entries, int startPage, int itemsPerPage) {
        int offset = startPage * itemsPerPage - itemsPerPage;
        return entries.Skip(offset).Take(5 * itemsPerPage + Math.Min(0, offset)).ToList();
    }
}
