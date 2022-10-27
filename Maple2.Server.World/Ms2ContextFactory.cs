using System;
using Maple2.Database.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maple2.Server.World;

public sealed class Ms2ContextFactory : IDesignTimeDbContextFactory<Ms2Context> {
    public Ms2Context CreateDbContext(string[] args) {
        string? gameDbConnection = Environment.GetEnvironmentVariable("GAME_DB_CONNECTION");
        if (gameDbConnection == null) {
            throw new ArgumentException("GAME_DB_CONNECTION environment variable was not set");
        }

        DbContextOptions options = new DbContextOptionsBuilder()
            .UseMySql(gameDbConnection, ServerVersion.AutoDetect(gameDbConnection), options => {
                options.MigrationsAssembly(typeof(Ms2ContextFactory).Assembly.FullName);
            }).Options;
        return new Ms2Context(options);
    }
}
