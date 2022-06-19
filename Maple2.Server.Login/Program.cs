using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Maple2.Server.Login;
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

await new HostBuilder()
    .ConfigureLogging(logging => {
        logging.ClearProviders();
        logging.AddSerilog(dispose: true);
    })
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureServices(Startup.ConfigureServices)
    .ConfigureContainer<ContainerBuilder>(Startup.ConfigureContainer)
    .RunConsoleAsync();
