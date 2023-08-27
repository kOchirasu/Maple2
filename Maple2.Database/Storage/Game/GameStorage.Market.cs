using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using MesoListing = Maple2.Model.Game.MesoListing;
using PremiumMarketItem = Maple2.Model.Game.PremiumMarketItem;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        // TODO: Should this filter out your own listings?
        public ICollection<MesoListing> SearchMesoListings(int pageSize, long minAmount = 0, long maxAmount = long.MaxValue) {
            return Context.MesoMarket.Where(listing => listing.ExpiryTime > DateTime.Now)
                .Where(listing => listing.Amount >= minAmount)
                .Where(listing => listing.Amount <= maxAmount)
                .OrderBy(listing => listing.Price)
                .Take(pageSize)
                .AsEnumerable()
                .Select<Model.MesoListing, MesoListing>(listing => listing)
                .ToList();
        }

        public ICollection<MesoListing> GetMyMesoListings(long accountId) {
            return Context.MesoMarket.Where(listing => listing.AccountId == accountId)
                .AsEnumerable()
                .Select<Model.MesoListing, MesoListing>(listing => listing)
                .ToList();
        }

        public MesoListing? GetMesoListing(long listingId) {
            return Context.MesoMarket.Find(listingId);
        }

        public MesoListing? CreateMesoListing(MesoListing listing) {
            Model.MesoListing model = listing;
            Context.MesoMarket.Add(model);

            return Context.TrySaveChanges() ? model : null;
        }

        public bool DeleteMesoListing(long listingId, bool sold = false) {
            Model.MesoListing? listing = Context.MesoMarket.Find(listingId);
            if (listing == null) {
                return false;
            }

            if (sold) {
                Model.SoldMesoListing soldListing = listing;
                Context.MesoMarket.Remove(listing);
                Context.MesoMarketSold.Add(soldListing);
                return Context.TrySaveChanges();
            }

            Context.MesoMarket.Remove(listing);
            return Context.TrySaveChanges();
        }

        public bool SavePremiumMarketEntry() {
            for (int i = 60000001; i < 60000064; i++) {
                Model.PremiumMarketItem item = new Model.PremiumMarketItem() {
                    ItemId = i,
                    TabId = 40300,
                    Rarity = 1,
                    Quantity = 1,
                    BonusQuantity = 0,
                    ItemDuration = 0,
                    CurrencyType = MeretMarketCurrencyType.Meret,
                    Price = 10,
                    SalePrice = 10,
                    SellBeginTime = new DateTime(2010, 1, 1),
                    SellEndTime = new DateTime(2030, 1, 1),
                    SalesCount = 0,
                    Label = MeretMarketItemLabel.None,
                    JobRequirement = JobFlag.Knight | JobFlag.Berserker | JobFlag.Priest | JobFlag.Wizard | JobFlag.Archer | JobFlag.HeavyGunner | JobFlag.Thief | JobFlag.Assassin | JobFlag.Newbie | JobFlag.RuneBlader | JobFlag.Striker | JobFlag.SoulBinder,
                    RequireAchievementId = 0,
                    RequireAchievementRank = 0,
                    RestockUnavailable = false,
                    RequireMinLevel = 0,
                    PromoData = null,
                    BannerName = "",
                    RequireMaxLevel = 0,
                    PcCafe = false,
                    ShowSaleTime = true,
                    CreationTime = DateTime.Now,
                };
                Context.PremiumMarketItem.Add(item);
            }

            return Context.TrySaveChanges();
        }

        public List<PremiumMarketItem> GetAllPremiumMarketEntries() {
            //TODO: Fix this before PR
            return Context.PremiumMarketItem
                .AsEnumerable()
                .Select(ToMarketEntry)
                .Where(entry => entry != null)
                .ToList()!;
        }

        public ICollection<MarketItem> GetPremiumMarketEntries(bool sortGender, bool sortJob, GenderFlag gender, JobFlag job, string searchString, params int[] tabIds) {
            IEnumerable<MarketItem> results;
            if (tabIds.Length == 0) {
                results = Context.PremiumMarketItem
                    .AsEnumerable()
                    .Select(ToMarketEntry)
                    .Where(entry => entry != null &&
                                    entry.ParentId == 0 &&
                                    (entry.ItemMetadata.Name != null && entry.ItemMetadata.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)) &&
                                    gender.HasFlag(entry.ItemMetadata.Limit.Gender.Flag()) &&
                                    (entry.ItemMetadata.Limit.JobLimits.Length == 0 || entry.ItemMetadata.Limit.JobLimits.Any(jobs => job.Code().Any(codes => codes == jobs)))
                    )
                    .ToList()!;

                // Get any additional quantities
                foreach (MarketItem marketEntry in results) {
                    if (marketEntry is not PremiumMarketItem premium) {
                        continue;
                    }
                    premium.AdditionalQuantities = Context.PremiumMarketItem
                        .Select(ToMarketEntry)
                        .Where(subEntry => subEntry != null &&
                                           subEntry.ParentId == premium.Id)
                        .ToList()!;
                }
            } else {
                results = Context.PremiumMarketItem
                    .AsEnumerable()
                    .Select(ToMarketEntry)
                    .Where(entry => entry != null &&
                                    entry.ParentId == 0 &&
                                    tabIds.Contains(entry.TabId) &&
                                    (entry.ItemMetadata.Name != null && entry.ItemMetadata.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)) &&
                                    gender.HasFlag(entry.ItemMetadata.Limit.Gender.Flag()) &&
                                    (entry.ItemMetadata.Limit.JobLimits.Length == 0 || entry.ItemMetadata.Limit.JobLimits.Any(jobs => job.Code().Any(codes => codes == jobs)))
                    )
                    .ToList()!;

                // Get any additional quantities
                foreach (MarketItem marketEntry in results) {
                    if (marketEntry is not PremiumMarketItem premium) {
                        continue;
                    }
                    premium.AdditionalQuantities = Context.PremiumMarketItem
                        .Select(ToMarketEntry)
                        .Where(subEntry => subEntry != null &&
                                           subEntry.ParentId == premium.Id)
                        .ToList()!;
                }
            }


            if (sortGender) {
                results = results.OrderBy(item => item.ItemMetadata.Limit.Gender);
            }
            if (sortJob) {
                results = results.OrderBy(item => item.ItemMetadata.Limit.JobLimits);
            }
            return results.ToList();
        }

        public ICollection<MarketItem> GetMarketItems(MeretMarketSection section, int tabId) {
            IEnumerable<MarketItem> results = new List<MarketItem>();
            switch (section) {
                case MeretMarketSection.All:
                    results = Context.PremiumMarketItem
                        .AsEnumerable()
                        .Select(ToMarketEntry)
                        .Where(entry => entry != null &&
                                        entry.ParentId == 0 &&
                                        entry.TabId == tabId)
                        .ToList()!;
                    break;
            }

            foreach (MarketItem marketEntry in results) {
                if (marketEntry is not PremiumMarketItem premium) {
                    continue;
                }
                premium.AdditionalQuantities = Context.PremiumMarketItem
                    .Select(ToMarketEntry)
                    .Where(subEntry => subEntry != null &&
                                       subEntry.ParentId == premium.Id)
                    .ToList()!;
            }
            return results.ToList();
        }

        public PremiumMarketItem? GetPremiumMarketEntry(int id) {
            Model.PremiumMarketItem? model = Context.PremiumMarketItem.Find(id);
            if (model == null) {
                return null;
            }

            return game.itemMetadata.TryGet(model.ItemId, out ItemMetadata? metadata) ? model.Convert(metadata) : null;
        }

        private PremiumMarketItem? ToMarketEntry(Model.PremiumMarketItem? model) {
            if (model == null) {
                return null;
            }

            return game.itemMetadata.TryGet(model.ItemId, out ItemMetadata? metadata) ? model.Convert(metadata) : null;
        }
    }
}
