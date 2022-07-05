using System;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class Plot {
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public int MapId { set; get; }
    public int Number { get; set; }

    public string Name { get; set; } = string.Empty;
    public PlotState State { get; set; }

    public DateTime ExpiryTime { get; init; }
    public DateTime LastModified { get; init; }

    public static implicit operator Plot?(Maple2.Model.Game.Plot? other) {
        return other == null ? null : new Plot {
            Id = other.Uid,
            OwnerId = other.OwnerId,
            MapId = other.MapId,
            Number = other.Number,
            Name = other.Name,
            State = other.State,
            ExpiryTime = other.ExpiryTime.FromEpochSeconds(),
        };
    }

    public Maple2.Model.Game.Plot Convert(UgcMapGroup metadata) {
        return new Maple2.Model.Game.Plot(metadata) {
            Uid = Id,
            OwnerId = OwnerId,
            MapId = MapId,
            Number = Number,
            Name = Name,
            State = State,
            ExpiryTime = ExpiryTime.ToEpochSeconds(),
            LastModified = LastModified.ToEpochSeconds(),
        };
    }

    public static void Configure(EntityTypeBuilder<Plot> builder) {
        builder.HasKey(plot => plot.Id);
        builder.HasIndex(plot => plot.MapId);

        builder.OneToOne<Plot, Account>()
            .HasForeignKey<Plot>(plot => plot.OwnerId);

        builder.Property(plot => plot.LastModified)
            .ValueGeneratedOnAddOrUpdate();
    }
}
