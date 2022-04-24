using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Extensions;

internal static class PropertyBuilderExtensions {
    public static PropertyBuilder<TProperty> HasJsonConversion<TProperty>(this PropertyBuilder<TProperty> builder) {
        return builder.HasConversion(
            property => JsonSerializer.Serialize(property, (JsonSerializerOptions) null),
            value => JsonSerializer.Deserialize<TProperty>(value, (JsonSerializerOptions) null)
        ).HasColumnType("json");
    }
}
