using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using MesoListing = Maple2.Model.Game.MesoListing;

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
    }
}
