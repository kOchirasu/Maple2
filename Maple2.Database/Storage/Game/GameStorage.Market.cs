using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model;
using Maple2.Model.Enum;
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

        public ICollection<PremiumMarketItem> GetMarketItems() {
            IEnumerable<PremiumMarketItem> selectedResults = Context.PremiumMarketItem
                .Where(entry => entry.ParentId == 0)
                .AsEnumerable()
                .Select(ToMarketEntry)
                .ToList()!;

            selectedResults = GetAdditionalQuantities(selectedResults);
            return selectedResults.ToList();
        }

        private IList<PremiumMarketItem> GetAdditionalQuantities(IEnumerable<PremiumMarketItem> selectedItems) {
            foreach (PremiumMarketItem marketEntry in selectedItems) {
                marketEntry.AdditionalQuantities = Context.PremiumMarketItem
                    .Where(subEntry => subEntry.ParentId == marketEntry.Id)
                    .AsEnumerable()
                    .Select(ToMarketEntry)
                    .ToList()!;
            }
            return selectedItems.ToList();
        }

        private PremiumMarketItem? ToMarketEntry(Model.PremiumMarketItem? model) {
            if (model == null) {
                return null;
            }

            return game.itemMetadata.TryGet(model.ItemId, out ItemMetadata? metadata) ? model.Convert(metadata) : null;
        }
    }
}
