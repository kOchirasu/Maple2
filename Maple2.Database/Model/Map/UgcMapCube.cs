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
    public long ItemUid { get; set; }
    public UgcItemLook? Template { get; set; }

    public static implicit operator (UgcItemCube Cube, Vector3B Position, float Rotation)(UgcMapCube other) {
        return (new UgcItemCube(other.ItemId, other.ItemUid, other.Template), new Vector3B(other.X, other.Y, other.Z), other.Rotation);
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
