using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Context;
using Maple2.Model.Metadata;

namespace Maple2.Database.Storage;

public class ScriptMetadataStorage : MetadataStorage<int, ScriptMetadata> {
    private const int CACHE_SIZE = 7000; // ~6.5k total items

    public ScriptMetadataStorage(MetadataContext context) : base(context, CACHE_SIZE) { }

    public bool TryGet(int id, [NotNullWhen(true)] out ScriptMetadata? script) {
        if (Cache.TryGet(id, out script)) {
            return true;
        }

        lock (Context) {
            // Double-checked locking
            if (Cache.TryGet(id, out script)) {
                return true;
            }

            script = Context.ScriptMetadata.Find(id);

            if (script == null) {
                return false;
            }

            Cache.AddReplace(id, script);
        }

        return true;
    }
}
