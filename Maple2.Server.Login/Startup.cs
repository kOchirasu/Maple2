using System.Reflection;
using Autofac;
using Maple2.Server.Core.Modules;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.DependencyInjection;

namespace Maple2.Server.Login;

public static class Startup {
    // ConfigureServices is where you register dependencies. This gets called by the runtime before the
    // ConfigureContainer method, below.
    public static void ConfigureServices(IServiceCollection services) {
        services.RegisterModule<WorldClientModule>();

        services.AddSingleton<LoginServer>();
        services.AddHostedService<LoginServer>(provider => provider.GetService<LoginServer>()!);
    }

    // ConfigureContainer is where you can register things directly with Autofac. This runs after
    // ConfigureServices so the things here will override registrations made in ConfigureServices.
    // Don't build the container; that gets done for you by the factory.
    public static void ConfigureContainer(ContainerBuilder builder) {
        builder.RegisterType<PacketRouter<LoginSession>>()
            .As<PacketRouter<LoginSession>>()
            .SingleInstance();
        builder.RegisterType<LoginSession>()
            .PropertiesAutowired()
            .AsSelf();

        // Database
        builder.RegisterModule<GameDbModule>();
        builder.RegisterModule<DataDbModule>();

        // Make all packet handlers available to PacketRouter
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(type => typeof(PacketHandler<LoginSession>).IsAssignableFrom(type))
            .As<PacketHandler<LoginSession>>()
            .PropertiesAutowired()
            .SingleInstance();
    }
}
