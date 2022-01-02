using Autofac;
using Maple2.Server.Core.Modules;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.DependencyInjection;

namespace Maple2.Server.Login;

public static class Startup {
    // ConfigureServices is where you register dependencies. This gets called by the runtime before the
    // ConfigureContainer method, below.
    public static void ConfigureServices(IServiceCollection services) {
        // services.AddGrpcClient<World.Service.World.WorldClient>(options => {
        //     options.Address = new Uri("https://localhost:5001");
        //     options.ChannelOptionsActions.Add(options => {
        //         // Return "true" to allow certificates that are untrusted/invalid
        //         options.HttpHandler = new HttpClientHandler {
        //             ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        //         };
        //     });
        // });

        services.AddSingleton<LoginServer>();
        services.AddHostedService<LoginServer>(provider => provider.GetService<LoginServer>());
    }

    // ConfigureContainer is where you can register things directly with Autofac. This runs after
    // ConfigureServices so the things here will override registrations made in ConfigureServices.
    // Don't build the container; that gets done for you by the factory.
    public static void ConfigureContainer(ContainerBuilder builder) {
        builder.RegisterModule<NLogModule>();

        builder.RegisterType<PacketRouter<LoginSession>>()
            .As<PacketRouter<LoginSession>>()
            .SingleInstance();
        builder.RegisterType<LoginSession>()
            .AsSelf();

        // Make all packet handlers available to PacketRouter
        builder.RegisterAssemblyTypes(typeof(LoginPacketHandler).Assembly)
            .Where(type => typeof(IPacketHandler<LoginSession>).IsAssignableFrom(type))
            .As<IPacketHandler<LoginSession>>()
            .SingleInstance();
    }
}
