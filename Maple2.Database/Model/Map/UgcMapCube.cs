﻿using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Extensions;
using Maple2.Model.Common;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class UgcMapCube {
    public long Id { get; set; }
    public long UgcMapId { get; set; }
    public sbyte X { get; set; }
    public sbyte Y { get; set; }
    public sbyte Z { get; set; }
    public float Rotation { get; set; }

    public int ItemId { get; set; }
    public UgcItemLook? Template { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator PlotCube?(UgcMapCube? other) {
        return other == null ? null : new PlotCube(other.ItemId, other.Id, other.Template) {
            Position = new Vector3B(other.X, other.Y, other.Z),
            Rotation = other.Rotation,
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator UgcMapCube?(PlotCube? other) {
        return other == null ? null : new UgcMapCube {
            Id = other.Id,
            X = other.Position.X,
            Y = other.Position.Y,
            Z = other.Position.Z,
            Rotation = other.Rotation,
            ItemId = other.ItemId,
            Template = other.Template,
        };
    }

    public static void Configure(EntityTypeBuilder<UgcMapCube> builder) {
        builder.ToTable("ugcmap-cube");
        builder.HasKey(cube => cube.Id);

        builder.HasOne<UgcMap>()
            .WithMany(ugcMap => ugcMap.Cubes)
            .HasForeignKey(cube => cube.UgcMapId);

        builder.Property(cube => cube.Template).HasJsonConversion();
    }
}
