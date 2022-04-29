using Maple2.Database.Extensions;
using Maple2.Database.Model.Metadata;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Context;

public sealed class MetadataContext : DbContext {
    public DbSet<TableChecksum> TableChecksum { get; set; }
    public DbSet<ItemMetadata> ItemMetadata { get; set; }
    public DbSet<NpcMetadata> NpcMetadata { get; set; }
    public DbSet<MapMetadata> MapMetadata { get; set; }

    public MetadataContext(DbContextOptions options) : base(options) {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TableChecksum>(Maple2.Database.Model.Metadata.TableChecksum.Configure);
        modelBuilder.Entity<ItemMetadata>(ConfigureItemMetadata);
        modelBuilder.Entity<NpcMetadata>(ConfigureNpcMetadata);
        modelBuilder.Entity<MapMetadata>(ConfigureMapMetadata);
    }
    
    private static void ConfigureItemMetadata(EntityTypeBuilder<ItemMetadata> builder) {
        builder.ToTable("item");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SlotNames).HasJsonConversion();
        builder.Property(item => item.Property).HasJsonConversion();
        builder.Property(item => item.Limit).HasJsonConversion();
    }
    
    private static void ConfigureNpcMetadata(EntityTypeBuilder<NpcMetadata> builder) {
        builder.ToTable("npc");
        builder.HasKey(npc => npc.Id);
        builder.Property(npc => npc.Stat).HasJsonConversion();
        builder.Property(npc => npc.Basic).HasJsonConversion();
        builder.Property(npc => npc.Action).HasJsonConversion();
        builder.Property(npc => npc.Dead).HasJsonConversion();
    }
    
    private static void ConfigureMapMetadata(EntityTypeBuilder<MapMetadata> builder) {
        builder.ToTable("map");
        builder.HasKey(map => map.Id);
        builder.Property(map => map.Property).HasJsonConversion();
        builder.Property(map => map.Limit).HasJsonConversion();
        builder.Property(map => map.CashCall).HasJsonConversion();
        builder.Property(map => map.EntranceBuffs).HasJsonConversion();
    }
}
