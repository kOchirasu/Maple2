using System;
using System.IO;
using Autofac.Extensions.DependencyInjection;
using Maple2.Database.Context;
using Maple2.Server.Core.Constants;
using Maple2.Server.World;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

IConfigurationRoot configRoot = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .Build();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configRoot)
    .CreateLogger();

IHostBuilder builder = Host.CreateDefaultBuilder()
    .ConfigureLogging(logging => {
        logging.ClearProviders();
        logging.AddSerilog(dispose: true);
    })
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>());

DbContextOptions options = new DbContextOptionsBuilder()
    .UseMySql(Target.GAME_DB_CONNECTION, ServerVersion.AutoDetect(Target.GAME_DB_CONNECTION)).Options;
await using (var initContext = new InitializationContext(options)) {
    // Initialize database if needed
    if (!initContext.Initialize()) {
        Log.Debug("Database has already been initialized");
    }
}

await builder.RunConsoleAsync();
