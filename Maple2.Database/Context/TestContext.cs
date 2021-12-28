using Maple2.Database.Schema;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Context;

public class TestContext : DbContext {
    internal DbSet<Account> Account { get; set; }
    internal DbSet<Character> Character { get; set; }

    public TestContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>(Schema.Account.Configure);
        modelBuilder.Entity<Character>(Schema.Character.Configure);
    }
}