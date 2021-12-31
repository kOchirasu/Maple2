using System.ComponentModel.DataAnnotations.Schema;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model.Metadata;

[Table("item")]
public record ItemMetadata(
    int Id,
    string Name,
    EquipSlot[] SlotNames,
    ItemMetadataProperty Property,
    ItemMetadataLimit Limit) {

    internal static void Configure(EntityTypeBuilder<ItemMetadata> builder) {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SlotNames).HasJsonConversion();
        builder.Property(item => item.Property).HasJsonConversion();
        builder.Property(item => item.Limit).HasJsonConversion();
    }
}

public record ItemMetadataProperty(
    bool IsSkin,
    int SkinType, // 99 = Template
    int SlotMax,
    int Type,
    int SubType,
    int Group,
    int Collection,
    int GearScore,
    int TradableCount,
    int RepackCount,
    bool DisableDrop);

public record ItemMetadataLimit(
    Gender Gender,
    int Level,
    int TransferType, // [0-7]
    bool ShopSell,
    bool EnableBreak,
    bool EnableEnchant,
    bool EnableMeretMarket,
    bool EnableSocketTransfer,
    bool RequireVip,
    bool RequireWedding,
    JobCode[] Jobs);
