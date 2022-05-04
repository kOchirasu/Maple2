using System;
using Maple2.Database.Context;
using Maple2.Model.Metadata;

namespace Maple2.Database.Storage;

public class TableMetadataStorage {
    public readonly JobTable JobTable;

    public TableMetadataStorage(MetadataContext context) {
        JobTable = Retrieve<JobTable>(context, "job.xml");
    }

    private static T Retrieve<T>(MetadataContext context, string key) where T : Table {
        lock (context) {
            TableMetadata row = context.TableMetadata.Find(key);
            if (row == null) {
                throw new InvalidOperationException($"Row does not exist: {key}");
            }

            return row.Table as T;
        }
    }
}
