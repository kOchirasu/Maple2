using Maple2.Model.Common;
using Maple2.Model.User;
using Maple2.Tools.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Config; 

internal class CharacterConfig  : IEntityTypeConfiguration<Character> {
    public void Configure(EntityTypeBuilder<Character> builder) {
        builder.Property(character => character.LastModified).IsConcurrencyToken();
        builder.HasKey(character => character.Id);
        builder.HasOne<Account>()
            .WithMany(account => account.Characters)
            .HasForeignKey(character => character.AccountId);
        builder.HasIndex(character => character.Name).IsUnique();
        builder.Property(character => character.CreationTime)
            .ValueGeneratedOnAdd();
        builder.Property(character => character.SkinColor).HasConversion(
            color => color.Serialize(),
            bytes => bytes.Deserialize<SkinColor>()
        );
    }
}