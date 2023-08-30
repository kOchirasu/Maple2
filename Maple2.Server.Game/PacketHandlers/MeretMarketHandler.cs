using System;
using System.Collections;
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
using Microsoft.Scripting.Utils;

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
        MeretMarketSection section = GetMarketSection(packet.ReadByte());
        packet.ReadByte();
        byte itemsPerPage = packet.ReadByte();

        ICollection<MarketItem> entries = GetItems(session, section, sortBy, tabId, gender, job, searchString);
        int totalItems = entries.Count;
        entries = TakeLimit(entries, startPage, itemsPerPage);

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

        if (entry.JobRequirement.Code().All(job => job != session.Player.Value.Character.Job.Code())) {
            return;
        }

        if (entry.RequireAchievementId > 0 && session.Achievement.HasAchievement(entry.RequireAchievementId, entry.RequireAchievementRank)) {
            return;
        }

        price = entry.SalePrice > 0 ? entry.SalePrice : entry.Price;
        StringCode payResult = Pay(session, price, entry.CurrencyType);
        if (payResult != StringCode.s_empty_string) {
            session.Send(NoticePacket.MessageBox(payResult));
            return;
        }

        Item? newItem = session.Item.CreateItem(entry.ItemMetadata.Id, entry.Rarity, entry.Quantity + entry.BonusQuantity);
        if (newItem == null) {
            return;
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

        ICollection<MarketItem> entries = new List<MarketItem>();
        using GameStorage.Request db = session.GameStorage.Context();
        switch (GetMarketSection(section)) {
            case MeretMarketSection.All:
                entries = session.GetPremiumMarketItems(tabId).Cast<MarketItem>().ToList();
                break;
            case MeretMarketSection.Premium:
            case MeretMarketSection.Ugc:
            case MeretMarketSection.RedMeret:
                break;
        }

        // Featured page needs a multiple of 2 slots. Add to the entries total if odd number.
        byte marketSlots = (byte) (entries.Count % 2 == 0 ? entries.Count : (byte) (entries.Count + 1));
        session.Send(MeretMarketPacket.Featured(section, tabId, marketSlots, entries.ToList()));
    }

    private void HandleFindItem(GameSession session, IByteReader packet) {
        bool premium = packet.ReadBool();
        MarketItem? marketItem;
        using GameStorage.Request db = session.GameStorage.Context();
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

        session.Send(MeretMarketPacket.LoadItems(new List<MarketItem> {
            marketItem
        }, 1, 1));
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
        MeretMarketSection section = GetMarketSection(packet.ReadByte());

        ICollection<MarketItem> entries = GetItems(session, section, sortBy, 0, gender, job, searchString);
        int totalItems = entries.Count;
        entries = TakeLimit(entries, startPage, itemsPerPage);

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

    private ICollection<MarketItem> GetItems(GameSession session, MeretMarketSection section, MeretMarketSort sortBy, int tabId, GenderFilterFlag genderFilter, JobFilterFlag jobFilter, string searchString) {
        using GameStorage.Request db = session.GameStorage.Context();
        if (tabId == 0) {
            return Filter(session.GetPremiumMarketItems(), genderFilter, jobFilter, searchString).Cast<MarketItem>().ToList();
        }
        if (!TableMetadata.MeretMarketCategoryTable.Entries.TryGetValue((int) section, out IReadOnlyDictionary<int, MeretMarketCategoryTable.Tab>? sectionDictionary) ||
            !sectionDictionary.TryGetValue(tabId, out MeretMarketCategoryTable.Tab? tab)) {
            return new List<MarketItem>();
        }

        // get any sub tabs
        int[] tabIds = new[] {tabId}.Concat(tab.SubTabIds).ToArray();

        ICollection<PremiumMarketItem> results = session.GetPremiumMarketItems(tabIds);
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
            case MeretMarketCurrencyType.RedMeret: // TODO: Implement Red Meret
                return StringCode.s_err_lack_merat_red;
            default:
                return StringCode.s_err_lack_money;
        }
    }

    private static ICollection<PremiumMarketItem> Filter(IEnumerable<PremiumMarketItem> items, GenderFilterFlag gender, JobFilterFlag job, string searchString) {
        return items.Where(entry =>
                (entry.ItemMetadata.Name != null && entry.ItemMetadata.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)) &&
                gender.HasFlag(entry.ItemMetadata.Limit.Gender.Flag()) &&
                (entry.ItemMetadata.Limit.JobLimits.Length == 0 || entry.ItemMetadata.Limit.JobLimits.Any(jobs => job.Code().Any(codes => codes == jobs))))
            .ToList();
    }

    private static ICollection<MarketItem> Sort(IEnumerable<MarketItem> entries, MeretMarketSort sort, bool sortJob, bool sortGender) {
        if (sortGender) {
            entries = entries.OrderBy(item => item.ItemMetadata.Limit.Gender);
        }
        if (sortJob) {
            entries = entries.OrderBy(item => item.ItemMetadata.Limit.JobLimits);
        }

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
