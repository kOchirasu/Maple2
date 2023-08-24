using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
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
        
        public List<PremiumMarketEntry> GetAllPremiumMarketEntries() {
            //TODO: Fix this before PR
            /*var list = new List<PremiumMarketEntry>();
            foreach (Model.PremiumMarketEntry entry in Context.PremiumMarketEntry) {
                if (!game.itemMetadata.TryGet(entry.ItemId, out ItemMetadata? metadata)) {
                    continue;
                }
                
                list.Add(new PremiumMarketEntry(entry.Id, metadata));

            }*/
            /*if (tabId == 0) {
                return Context.PremiumMarketEntry
                    .AsEnumerable()
                    .Select(ToMarketEntry)
                    .Where(entry => entry != null)
                    .ToList()!;
            }*/
            return Context.PremiumMarketEntry
                .AsEnumerable()
                .Select(ToMarketEntry)
                .Where(entry => entry != null)
                .ToList()!;
            
            List<PremiumMarketEntry?> results = Context.PremiumMarketEntry
                .AsEnumerable()
                .Select(ToMarketEntry)
                .Where(entry => entry != null && 
                                entry.ParentId == 0)
                .ToList();
            
            List<PremiumMarketEntry?> subResults = Context.PremiumMarketEntry
                .AsEnumerable()
                .Select(ToMarketEntry)
                .Where(entry => entry != null && 
                                entry.ParentId != 0)
                .ToList();

            foreach (PremiumMarketEntry? entry in subResults) {
                if (entry == null) {
                    continue;
                }
                results.FirstOrDefault(item => item?.Id == entry.ParentId)?.AdditionalQuantities.Add(entry);
            }

            return results!;
        }
        
        private PremiumMarketEntry? ToMarketEntry(Model.PremiumMarketEntry? model) {
            if (model == null) {
                return null;
            }

            return game.itemMetadata.TryGet(model.ItemId, out ItemMetadata? metadata) ? model.Convert(metadata) : null;
        }
    }
}
