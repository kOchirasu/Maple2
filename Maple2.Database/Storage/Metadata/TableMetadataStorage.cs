using System;
using Maple2.Database.Context;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage; 

public class TableMetadataStorage {
    public readonly JobTable JobTable;

    public TableMetadataStorage(DbContextOptions options) {
        JobTable = Retrieve<JobTable>(options, "job.xml");
    }

    private static T Retrieve<T>(DbContextOptions options, string key) where T : Table {
        using var context = new MetadataContext(options);
        TableMetadata row = context.TableMetadata.Find(key);
        if (row == null) {
            throw new InvalidOperationException($"Row does not exist: {key}");
        }
        
        return row.Table as T;
    }
}
