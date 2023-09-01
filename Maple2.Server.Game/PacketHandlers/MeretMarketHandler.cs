using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.PacketHandlers;

public class MeretMarketHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.MeretMarket;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
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
        var gender = packet.Read<GenderFilterFlag>();
        var job = packet.Read<JobFilterFlag>();
        var sortBy = packet.Read<MeretMarketSort>();
        string searchString = packet.ReadUnicodeString();
        int startPage = packet.ReadInt();
        int unknown = packet.ReadInt(); // repeats the startPage value
        MeretMarketSection section = ToMarketSection(packet.ReadByte());
        packet.ReadByte();
        byte itemsPerPage = packet.ReadByte();

        ICollection<MarketItem> entries = GetItems(session, section, sortBy, tabId, gender, job, searchString).ToList();
        int totalItems = entries.Count;
        entries = TakeLimit(entries, startPage, itemsPerPage);

        session.Send(MeretMarketPacket.LoadItems(entries, totalItems, startPage));
    }

    private void HandlePurchase(GameSession session, IByteReader packet) {
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
        packet.ReadLong(); // price? (but price is auto-determined below)

        PremiumMarketItem? entry = session.GetPremiumMarketItem(premiumMarketId, childMarketItemId);
        if (entry == null) {
            return;
        }

        // TODO: Find meret market error packets
        if (entry.SellBeginTime > DateTime.Now.ToEpochSeconds() || entry.SellEndTime < DateTime.Now.ToEpochSeconds()) {
            return;
        }

        if ((entry.RequireMinLevel > 0 && entry.RequireMinLevel > session.Player.Value.Character.Level) ||
            (entry.RequireMaxLevel > 0 && entry.RequireMaxLevel < session.Player.Value.Character.Level)) {
            return;
        }

        // If JobRequirement is None, no job is eligible.
        if ((entry.JobRequirement & session.Player.Value.Character.Job.Code().FilterFlag()) == JobFilterFlag.None) {
            return;
        }

        if (entry.RequireAchievementId > 0 && session.Achievement.HasAchievement(entry.RequireAchievementId, entry.RequireAchievementRank)) {
            return;
        }

        long price = entry.SalePrice > 0 ? entry.SalePrice : entry.Price;
        StringCode payResult = Pay(session, price, entry.CurrencyType);
        if (payResult != StringCode.s_empty_string) {
            session.Send(NoticePacket.MessageBox(payResult));
            return;
        }

        Item? newItem = session.Item.CreateItem(entry.ItemMetadata.Id, entry.Rarity, entry.Quantity + entry.BonusQuantity);
        if (newItem == null) {
            Logger.Fatal("Failed to create item {ItemId}, {Rarity}, {Quantity}", entry.ItemMetadata.Id, entry.Rarity, entry.Quantity + entry.BonusQuantity);
            throw new InvalidOperationException($"Fatal: Failed to create item {entry.ItemMetadata.Id}, {entry.Rarity}, {entry.Quantity + entry.BonusQuantity}");
        }

        if (entry.ItemDuration > 0) {
            newItem.ExpiryTime = DateTime.Now.AddDays(entry.ItemDuration).ToEpochSeconds();
        }

        if (!session.Item.Inventory.Add(newItem, true)) {
            session.Item.MailItem(newItem);
        }

        session.Send(MeretMarketPacket.Purchase(totalQuantity, itemIndex, price, premiumMarketId, ugcItemId));
    }

    private static void HandleFeatured(GameSession session, IByteReader packet) {
        byte section = packet.ReadByte();
        byte tabId = packet.ReadByte();

        IList<MarketItem> entries = ToMarketSection(section) switch {
            MeretMarketSection.All => session.GetPremiumMarketItems(tabId).Cast<MarketItem>().ToList(),
            MeretMarketSection.Premium => session.GetPremiumMarketItems(tabId).Cast<MarketItem>().ToList(),
            MeretMarketSection.Ugc => new List<MarketItem>(),
            MeretMarketSection.RedMeret => new List<MarketItem>(),
            _ =>  new List<MarketItem>(),
        };

        // Featured page needs a multiple of 2 slots. Add to the entries total if odd number.
        byte marketSlots = (byte) (entries.Count % 2 == 0 ? entries.Count : (byte) (entries.Count + 1));
        session.Send(MeretMarketPacket.Featured(section, tabId, marketSlots, entries));
    }

    private void HandleFindItem(GameSession session, IByteReader packet) {
        bool premium = packet.ReadBool();
        MarketItem? marketItem;
        if (premium) {
            int premiumId = packet.ReadInt();
            marketItem = session.GetPremiumMarketItem(premiumId);
            if (marketItem == null) {
                return;
            }
        } else {
            long ugcId = packet.ReadLong();
            // TODO: Implement UGC Items
            return;
        }

        session.Send(MeretMarketPacket.LoadItems(new List<MarketItem> {marketItem}, 1, 1));
    }

    private void HandleSearch(GameSession session, IByteReader packet) {
        packet.ReadInt(); // 1
        var gender = packet.Read<GenderFilterFlag>();
        var job = packet.Read<JobFilterFlag>();
        var sortBy = packet.Read<MeretMarketSort>();
        string searchString = packet.ReadUnicodeString();
        int startPage = packet.ReadInt(); // 1
        packet.ReadInt(); // 1
        packet.ReadByte();
        packet.ReadByte();
        byte itemsPerPage = packet.ReadByte();
        MeretMarketSection section = ToMarketSection(packet.ReadByte());

        ICollection<MarketItem> entries = GetItems(session, section, sortBy, 0, gender, job, searchString).ToList();
        int totalItems = entries.Count;
        entries = TakeLimit(entries, startPage, itemsPerPage);

        session.Send(MeretMarketPacket.LoadItems(entries, totalItems, startPage));
    }

    private static MeretMarketSection ToMarketSection(byte section) {
        return section switch {
            0 => MeretMarketSection.All,
            1 => MeretMarketSection.Premium,
            2 => MeretMarketSection.RedMeret,
            3 => MeretMarketSection.Ugc,
            _ => MeretMarketSection.All,
        };
    }

    #region Helpers
    private IEnumerable<MarketItem> GetItems(GameSession session, MeretMarketSection section, MeretMarketSort sortBy, int tabId, GenderFilterFlag genderFilter, JobFilterFlag jobFilter, string searchString) {
        if (tabId == 0) {
            return Filter(session.GetPremiumMarketItems(), genderFilter, jobFilter, searchString);
        }
        if (!TableMetadata.MeretMarketCategoryTable.Entries.TryGetValue((int) section, tabId, out MeretMarketCategoryTable.Tab? tab)) {
            return new List<MarketItem>();
        }

        // get any sub tabs
        int[] tabIds = new[] {tabId}.Concat(tab.SubTabIds).ToArray();

        IEnumerable<PremiumMarketItem> results = session.GetPremiumMarketItems(tabIds);
        results = Filter(results, genderFilter, jobFilter, searchString);
        return Sort(results, sortBy, tab.SortGender, tab.SortJob);
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
            case MeretMarketCurrencyType.RedMeret:
                if (session.Currency.CanAddGameMeret(-price) != -price) {
                    return StringCode.s_err_lack_merat_red;
                }
                session.Currency.GameMeret -= price;
                return StringCode.s_empty_string;
            default:
                return StringCode.s_err_lack_money;
        }
    }

    private static IEnumerable<PremiumMarketItem> Filter(IEnumerable<PremiumMarketItem> items, GenderFilterFlag gender, JobFilterFlag job, string searchString) {
        if (!string.IsNullOrWhiteSpace(searchString)) {
            items = items.Where(entry => entry.ItemMetadata.Name != null && entry.ItemMetadata.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }
        if (gender != GenderFilterFlag.All) {
            items = items.Where(entry => gender.HasFlag(entry.ItemMetadata.Limit.Gender.FilterFlag()));
        }

        // JobFilterFlag.None means no jobs are eligible.
        return items.Where(entry => (job & entry.ItemMetadata.Limit.JobLimits.FilterFlags()) != JobFilterFlag.None);
    }

    private static IEnumerable<MarketItem> Sort(IEnumerable<MarketItem> entries, MeretMarketSort sort, bool sortJob, bool sortGender) {
        if (sortGender) {
            entries = entries.OrderBy(item => item.ItemMetadata.Limit.Gender);
        }
        if (sortJob) {
            entries = entries.OrderBy(item => item.ItemMetadata.Limit.JobLimits);
        }

        switch (sort) {
            case MeretMarketSort.MostRecent:
                return entries.OrderByDescending(entry => entry.CreationTime);
            case MeretMarketSort.PriceHighest:
                return entries.OrderByDescending(entry => entry.Price);
            case MeretMarketSort.PriceLowest:
                return entries.OrderBy(entry => entry.Price);
            // TODO: Implement most popular?
            // Unsure how most popular is different than top seller.
            case MeretMarketSort.MostPopularPremium:
            case MeretMarketSort.MostPopularUgc:
            case MeretMarketSort.TopSeller:
                return entries.OrderByDescending(entry => entry.SalesCount);
            case MeretMarketSort.None:
            default:
                return entries;
        }
    }

    /// <summary>
    /// Limits the amount of market items returned to the client.
    /// </summary>
    /// <returns>Limited Market Items. 5 * itemsPerPage</returns>
    private static ICollection<MarketItem> TakeLimit(IEnumerable<MarketItem> entries, int startPage, int itemsPerPage) {
        const int numPages = 5;
        int offset = startPage * itemsPerPage - itemsPerPage;
        return entries.Skip(offset).Take(numPages * itemsPerPage + Math.Min(0, offset)).ToList();
    }
    #endregion
}
