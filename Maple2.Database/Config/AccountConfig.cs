using Maple2.Model.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Config {
    internal class AccountConfig : IEntityTypeConfiguration<Account> {
        public void Configure(EntityTypeBuilder<Account> builder) {
            builder.Property(account => account.LastModified)
                .IsConcurrencyToken();
            builder.HasKey(account => account.Id);
            builder.Property(account => account.MaxCharacters)
                .HasDefaultValue(4);
            builder.HasMany(account => account.Characters);
        }
    }
}
