using System;
using Maple2.Model.Enum;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class Buddy {
    public long Id { get; set; }
    public long CharacterId { get; set; }
    public long BuddyId { get; set; }
    public Character BuddyCharacter { get; set; }
    public BuddyType Type { get; set; }
    public string Message { get; set; }
    public string BlockMessage { get; set; }

    public DateTime LastModified { get; set; }

    public static implicit operator Buddy?(Maple2.Model.Game.Buddy? other) {
        return other == null ? null : new Buddy {
            LastModified = other.LastModified,
            Id = other.Id,
            BuddyId = other.BuddyInfo.CharacterId,
            Type = other.Type,
            Message = other.Message,
            BlockMessage = other.BlockMessage,
        };
    }

    public static void Configure(EntityTypeBuilder<Buddy> builder) {
        builder.HasKey(buddy => buddy.Id);

        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(buddy => buddy.CharacterId)
            .IsRequired();
        builder.HasOne<Character>(buddy => buddy.BuddyCharacter)
            .WithMany()
            .HasForeignKey(buddy => buddy.BuddyId)
            .IsRequired();

        builder.Property(buddy => buddy.LastModified).IsRowVersion();
    }
}
