using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Context;
using Maple2.Model.Metadata;

namespace Maple2.Database.Storage;

public class RideMetadataStorage : MetadataStorage<int, RideMetadata> {
    private const int CACHE_SIZE = 500; // ~500 total items

    public RideMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

    public bool TryGet(int id, [NotNullWhen(true)] out RideMetadata? ride) {
        if (Cache.TryGet(id, out ride)) {
            return true;
        }

        lock (Context) {
            ride = Context.RideMetadata.Find(id);
        }

        if (ride == null) {
            return false;
        }

        Cache.AddReplace(id, ride);
        return true;
    }
}
