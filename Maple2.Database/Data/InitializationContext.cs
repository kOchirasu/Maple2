using Maple2.Database.Model;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Data;

public class InitializationContext : DbContext {
    internal DbSet<Account> Account { get; set; }
    internal DbSet<Character> Character { get; set; }
    internal DbSet<Item> Item { get; set; }

    public InitializationContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>(Maple2.Database.Model.Account.Configure);
        modelBuilder.Entity<Character>(Maple2.Database.Model.Character.Configure);
        modelBuilder.Entity<Item>(Maple2.Database.Model.Item.Configure);
    }

    public bool Initialize() {
        bool created = Database.EnsureCreated();
        if (!created) {
            return false;
        }

        Database.ExecuteSqlRaw("ALTER TABLE account AUTO_INCREMENT = 100000000000");
        Database.ExecuteSqlRaw("ALTER TABLE `character` AUTO_INCREMENT = 120000000000");
        Database.ExecuteSqlRaw("ALTER TABLE item AUTO_INCREMENT = 200000000000");

        return true;
    }
}