using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class UgcMapLayout {
    public long Id { get; set; }

    public Vector3B Origin { get; set; }
    public Vector3B Dimensions { get; set; }
    public IList<Cube> Cubes { get; set; }

    public DateTime LastModified { get; set; }

    public static implicit operator UgcMapLayout?(Maple2.Model.Game.Plot? other) {
        return other == null ? null : new UgcMapLayout {
            Id = other.Id,
            Cubes = other.Cubes.Select(entry => new Cube {
                Position = entry.Key,
                Rotation = entry.Value.Rotation,
                ItemId = entry.Value.Cube.Id,
                ItemUid = entry.Value.Cube.Uid,
                HasTemplate = entry.Value.Cube.Template != null,
            }).ToList(),
        };
    }

    public static void Configure(EntityTypeBuilder<UgcMapLayout> builder) {
        builder.ToTable("ugcmap-layout");
        builder.HasKey(layout => layout.Id);

        builder.Property(layout => layout.Origin).HasJsonConversion();
        builder.Property(layout => layout.Dimensions).HasJsonConversion();
        builder.Property(layout => layout.Cubes).HasJsonConversion();

        builder.Property(layout => layout.LastModified)
            .ValueGeneratedOnAddOrUpdate();
        IMutableProperty origin = builder.Property(layout => layout.Origin)
            .ValueGeneratedOnAdd().Metadata;
        origin.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        IMutableProperty dimensions = builder.Property(layout => layout.Dimensions)
            .ValueGeneratedOnAdd().Metadata;
        dimensions.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }
}

internal class Cube {
    public Vector3B Position { get; set; }
    public float Rotation { get; set; }

    public int ItemId { get; set; }
    public long ItemUid { get; set; }
    public bool HasTemplate { get; set; }
}
