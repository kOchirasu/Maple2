using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Maple2.Server.Login;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Reflection;
using Maple2.Server.Core.Modules;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.DependencyInjection;

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
    .ConfigureServices(services => {
        services.RegisterModule<WorldClientModule>();
        services.AddSingleton<LoginServer>();
        services.AddHostedService<LoginServer>(provider => provider.GetService<LoginServer>()!);
    })
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(autofac => {
        autofac.RegisterType<PacketRouter<LoginSession>>()
            .As<PacketRouter<LoginSession>>()
            .SingleInstance();
        autofac.RegisterType<LoginSession>()
            .PropertiesAutowired()
            .AsSelf();

        // Database
        autofac.RegisterModule<GameDbModule>();
        autofac.RegisterModule<DataDbModule>();

        // Make all packet handlers available to PacketRouter
        autofac.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(type => typeof(PacketHandler<LoginSession>).IsAssignableFrom(type))
            .As<PacketHandler<LoginSession>>()
            .PropertiesAutowired()
            .SingleInstance();
    })
    .RunConsoleAsync();
