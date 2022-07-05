using System.Collections.Generic;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class Housing {
    public long AccountId { get; set; }
    public int WeeklyArchitectScore { get; set; }
    public int ArchitectScore { get; set; }
    public HomeSettings HomeSettings { get; set; }
    public long HomePlotId { get; set; }
    public long? MapPlotId { get; set; }

    public static void Configure(EntityTypeBuilder<Housing> builder) {
        builder.HasKey(housing => housing.AccountId);
        builder.OneToOne<Housing, Account>()
            .HasForeignKey<Housing>(housing => housing.AccountId);

        builder.OneToOne<Housing, Plot>()
            .HasForeignKey<Housing>(account => account.HomePlotId);
        builder.OneToOne<Housing, Plot>()
            .HasForeignKey<Housing>(account => account.MapPlotId);

        builder.Property(housing => housing.HomeSettings).HasJsonConversion();
    }
}

internal class HomeSettings {
    public string Message { get; set; } = "";
    public byte Area { get; set; }
    public byte Height { get; set; }

    // Interior Settings
    public byte Background { get; set; }
    public byte Lighting { get; set; }
    public byte Camera { get; set; }
    public IDictionary<HomePermission, HomePermissionSetting> Permissions { get; set; } = new Dictionary<HomePermission, HomePermissionSetting>();
}
