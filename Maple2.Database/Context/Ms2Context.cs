using Maple2.Database.Model;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Context;

public sealed class Ms2Context : DbContext {
    internal DbSet<Account> Account { get; set; }
    internal DbSet<Character> Character { get; set; }
    internal DbSet<Item> Item { get; set; }
    internal DbSet<Club> Club { get; set; }
    internal DbSet<ClubMember> ClubMember { get; set; }

    public Ms2Context(DbContextOptions options) : base(options) {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>(Maple2.Database.Model.Account.Configure);
        modelBuilder.Entity<Character>(Maple2.Database.Model.Character.Configure);
        modelBuilder.Entity<Item>(Maple2.Database.Model.Item.Configure);
    }
}