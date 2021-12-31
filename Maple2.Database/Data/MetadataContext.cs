using Maple2.Database.Model.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Data;

public sealed class MetadataContext : DbContext {
    public DbSet<TableChecksum> TableChecksum { get; set; }
    public DbSet<ItemMetadata> ItemMetadata { get; set; }

    public MetadataContext(DbContextOptions options) : base(options) {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TableChecksum>(Maple2.Database.Model.Metadata.TableChecksum.Configure);
        modelBuilder.Entity<ItemMetadata>(Maple2.Database.Model.Metadata.ItemMetadata.Configure);
    }
}
