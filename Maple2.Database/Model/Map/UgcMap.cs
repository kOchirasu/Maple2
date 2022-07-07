using System;
using Maple2.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class UgcMap {
    public long Id { get; set; }
    public long OwnerId { get; set; }

    public int MapId { get; set; }
    public int Number { get; set; }
    public int ApartmentNumber { get; set; }

    public DateTime ExpiryTime { get; set; }
    public DateTime LastModified { get; set; }

    public static implicit operator UgcMap?(Maple2.Model.Game.Plot? other) {
        return other == null ? null : new UgcMap {
            Id = other.Id,
            OwnerId = other.OwnerId,
            MapId = other.MapId,
            Number = other.Number,
            ApartmentNumber = other.ApartmentNumber,
            ExpiryTime = other.ExpiryTime.FromEpochSeconds(),
        };
    }

    public static void Configure(EntityTypeBuilder<UgcMap> builder) {
        builder.ToTable("ugcmap");
        builder.HasKey(map => map.Id);
        builder.HasIndex(map => map.OwnerId);
        builder.HasIndex(map => map.MapId);

        builder.OneToOne<UgcMap, UgcMapLayout>()
            .HasForeignKey<UgcMapLayout>(layout => layout.Id);

        builder.Property(layout => layout.LastModified)
            .IsRowVersion();
    }
}
