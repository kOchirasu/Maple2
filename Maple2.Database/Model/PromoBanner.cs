using System;
using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class PromoBanner {
    public int Id { get; set; }
    public string Name { get; set; }
    public PromoBannerType Type { get; set; }
    public string SubType { get; set; }
    public string Url { get; set; }
    public PromoBannerLanguage Language { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.PromoBanner?(PromoBanner? other) {
        return other == null ? null : new Maple2.Model.Game.PromoBanner(other.Id) {
            Name = other.Name,
            Type = other.Type,
            SubType = other.SubType,
            Url = other.Url,
            Language = other.Language,
            BeginTime = other.BeginTime.ToEpochSeconds(),
            EndTime = other.EndTime.ToEpochSeconds(),
        };
    }
    
    public static void Configure(EntityTypeBuilder<PromoBanner> builder) {
        builder.ToTable("banner");
        builder.HasKey(banner => banner.Id);
    }
}
