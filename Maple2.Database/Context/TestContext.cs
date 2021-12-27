using Maple2.Database.Config;
using Maple2.Model.User;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Context; 

public class TestContext : DbContext {
    public DbSet<Account> Account { get; set; }

    public TestContext(DbContextOptions options) : base(options) { }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new AccountConfig());
        modelBuilder.ApplyConfiguration(new CharacterConfig());
    }
}