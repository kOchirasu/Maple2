using System;
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
        Initialize = 22, // actually unknown.
        OpenShop = 27,
        SendMarketRequest = 29,
        Purchase = 30,
        OpenNewFeatured = 101,
        OpenDesignShop = 102,
        Search = 104,
        LoadCart = 107,
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

        ICollection<MarketEntry> entries = Search(session, section, tabId, gender, job, searchString);

        switch (sort) {
            case MeretMarketSort.MostRecent:
                entries = entries.OrderByDescending(entry => entry.CreationTime).ToList();
                break;
            case MeretMarketSort.PriceHighest:
                entries = entries.OrderByDescending(entry => entry.Price).ToList();
                break;
            case MeretMarketSort.PriceLowest:
                entries = entries.OrderBy(entry => entry.Price).ToList();
                break;
            // TODO: Implement most popular?
            // Unsure how most popular is different than top seller.
            case MeretMarketSort.MostPopularPremium:
            case MeretMarketSort.MostPopularUgc:
            case MeretMarketSort.TopSeller:
                entries = entries.OrderByDescending(entry => entry.SalesCount).ToList();
                break;
            case MeretMarketSort.None:
            default:
                break;
        }
        session.Send(MeretMarketPacket.LoadShopCategory(entries));
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

    private ICollection<MarketEntry> Search(GameSession session, MeretMarketSection section, int tabId, GenderFlag gender, JobFlag job, string searchString) {
        var entries = new List<MarketEntry>();
        if (!TableMetadata.MeretMarketCategoryTable.Entries.TryGetValue((int) section, out IReadOnlyDictionary<int, MeretMarketCategoryTable.Tab>? sectionDictionary) ||
            !sectionDictionary.TryGetValue(tabId, out MeretMarketCategoryTable.Tab? tab)) {
            return entries;
        }

        //First get entries from the tab
        List<PremiumMarketEntry> tabResults = session.GetPremiumMarketEntries(tabId).ToList();
        entries.AddRange(SortAndFilterEntries(tabResults, tab.SortGender, tab.SortJob, gender, job, searchString));

        //Then get entries from subtabs
        if (tab.SubTabIds.Count > 0) {
            foreach (int subTabId in tab.SubTabIds) {
                if (sectionDictionary.TryGetValue(subTabId, out MeretMarketCategoryTable.Tab? subTab)) {
                    List<PremiumMarketEntry> subTabResults = session.GetPremiumMarketEntries(subTabId).ToList();
                    entries.AddRange(SortAndFilterEntries(subTabResults, subTab.SortGender, subTab.SortJob, gender, job, searchString));
                }
            }
        }
        return entries;
    }

    private static IEnumerable<PremiumMarketEntry> SortAndFilterEntries(List<PremiumMarketEntry> results, bool sortGender, bool sortJob,
                                                                        GenderFlag gender, JobFlag job, string searchString) {
        //First sort results as indicated on metadata. This is done before filtering due to each tab having different sorting options.
        if (sortGender) {
            results = results.OrderBy(item => item.ItemMetadata.Limit.Gender).ToList();
        }
        if (sortJob) {
            results = results.OrderBy(item => item.ItemMetadata.Limit.JobLimits).ToList();
        }

        //Then filter results
        foreach (PremiumMarketEntry entry in results) {
            if (entry.ItemMetadata.Name == null || !entry.ItemMetadata.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }
            if (entry.ItemMetadata.Limit.JobLimits.Length > 0 && !entry.ItemMetadata.Limit.JobLimits.Any(jobs => job.Code().Any(codes => codes == jobs))) {
                continue;
            }

            if (!gender.HasFlag(entry.ItemMetadata.Limit.Gender.Flag())) {
                continue;
            }

            yield return entry;
        }
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
        
        PremiumMarketEntry? entry = session.GetPremiumMarketEntry(childMarketItemId == 0 ? premiumMarketId : childMarketItemId);
        if (entry == null) {
            return;
        }

        StringCode payResult = Pay(session, price, entry.CurrencyType);
        if (payResult != StringCode.s_empty_string) {
            session.Send(NoticePacket.MessageBox(payResult));
            return;
        }

        Item? newItem = session.Item.CreateItem(entry.Id, entry.Rarity, entry.Quantity + entry.BonusQuantity);
        if (newItem == null) {
            return;
        }

        if (entry.ItemDuration > 0) {
            newItem.ExpiryTime = (long) (DateTime.Now.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds + entry.ItemDuration;
        }

        if (!session.Item.Inventory.Add(newItem, true)) {
            session.Item.MailItem(newItem);
        }

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
}
