using Maple2.Database.Extensions;
using Maple2.Database.Model.Metadata;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        modelBuilder.Entity<ItemMetadata>(ConfigureItemMetadata);
    }
    
    private static void ConfigureItemMetadata(EntityTypeBuilder<ItemMetadata> builder) {
        builder.ToTable("item");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SlotNames).HasJsonConversion();
        builder.Property(item => item.Property).HasJsonConversion();
        builder.Property(item => item.Limit).HasJsonConversion();
    }
}
