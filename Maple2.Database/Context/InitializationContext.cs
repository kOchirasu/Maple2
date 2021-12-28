using Maple2.Database.Schema;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Context;

public class InitializationContext : DbContext {
    internal DbSet<Account> Account { get; set; }
    internal DbSet<Character> Character { get; set; }

    public InitializationContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>(Schema.Account.Configure);
        modelBuilder.Entity<Character>(Schema.Character.Configure);
    }

    public bool Initialize() {
        bool created = Database.EnsureCreated();
        if (!created) {
            return false;
        }

        Database.ExecuteSqlRaw("ALTER TABLE account AUTO_INCREMENT = 100000000000");
        Database.ExecuteSqlRaw("ALTER TABLE `character` AUTO_INCREMENT = 120000000000");

        return true;
    }
}