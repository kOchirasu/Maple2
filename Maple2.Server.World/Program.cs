using System;
using Autofac.Extensions.DependencyInjection;
using Maple2.Database.Context;
using Maple2.Server.Core.Constants;
using Maple2.Server.World;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

IHostBuilder builder = Host.CreateDefaultBuilder()
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>());

DbContextOptions options = new DbContextOptionsBuilder()
    .UseMySql(Target.GAME_DB_CONNECTION, ServerVersion.AutoDetect(Target.GAME_DB_CONNECTION)).Options;
await using (var initContext = new InitializationContext(options)) {
    // Initialize database if needed
    if (!initContext.Initialize()) {
        Console.WriteLine("Database has already been initialized");
    }
}

await builder.RunConsoleAsync();
