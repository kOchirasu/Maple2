using System.Collections.Generic;
using Maple2.Database.Extensions;
using Maple2.Model.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class PlotCubes {
    public long Id { get; set; }
    public IList<PlacedCube> Cubes { get; set; }

    public static void Configure(EntityTypeBuilder<PlotCubes> builder) {
        builder.ToTable("plot-cubes");
        builder.HasKey(plot => plot.Id);
        builder.OneToOne<PlotCubes, Plot>()
            .HasForeignKey<PlotCubes>(plot => plot.Id);

        builder.Property(plot => plot.Cubes).HasJsonConversion();
    }
}

internal class PlacedCube {
    public Vector3B Position { get; set; }
    public float Rotation { get; set; }

    public int ItemId { get; set; }
    public long ItemUid { get; set; }
    public bool HasTemplate { get; set; }
}
