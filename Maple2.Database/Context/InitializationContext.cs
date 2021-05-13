using Maple2.Database.Config;
using Maple2.Model.User;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Context {
    public class InitializationContext : DbContext {
        public DbSet<Account> Account { get; set; }
        public DbSet<Character> Character { get; set; }

        public InitializationContext(DbContextOptions options) : base(options) { }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new AccountConfig());
            modelBuilder.ApplyConfiguration(new CharacterConfig());
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
}