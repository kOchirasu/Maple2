using System.IO;
using Autofac.Extensions.DependencyInjection;
using Maple2.Server.Game;
using Microsoft.AspNetCore.Hosting;
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

await Host.CreateDefaultBuilder()
    .ConfigureLogging(logging => {
        logging.ClearProviders();
        logging.AddSerilog(dispose: true);
    })
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>())
    .RunConsoleAsync();
