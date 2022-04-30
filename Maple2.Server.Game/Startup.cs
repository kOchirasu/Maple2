using System.Reflection;
using Autofac;
using Maple2.Server.Core.Modules;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Service;
using Maple2.Server.Game.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game;

public class Startup {
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    // ConfigureServices is where you register dependencies. This gets called by the runtime before the
    // ConfigureContainer method, below.
    public void ConfigureServices(IServiceCollection services) {
        services.AddGrpc();
        services.RegisterModule<WorldClientModule>();

        services.AddSingleton<GameServer>();
        services.AddHostedService<GameServer>(provider => provider.GetService<GameServer>());
    }

    // ConfigureContainer is where you can register things directly with Autofac. This runs after
    // ConfigureServices so the things here will override registrations made in ConfigureServices.
    // Don't build the container; that gets done for you by the factory.
    public void ConfigureContainer(ContainerBuilder builder) {
        builder.RegisterModule<NLogModule>();

        builder.RegisterType<PacketRouter<GameSession>>()
            .As<PacketRouter<GameSession>>()
            .SingleInstance();
        builder.RegisterType<GameSession>()
            .PropertiesAutowired()
            .AsSelf();
        
        // Database
        builder.RegisterModule<GameDbModule>();
        builder.RegisterModule<DataDbModule>();

        // Make all packet handlers available to PacketRouter
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(type => typeof(PacketHandler<GameSession>).IsAssignableFrom(type))
            .As<PacketHandler<GameSession>>()
            .PropertiesAutowired()
            .SingleInstance();
        
        // Managers
        builder.RegisterType<FieldManager.Factory>()
            .SingleInstance();
    }

    // Configure is where you add middleware. This is called after ConfigureContainer. You can use
    // IApplicationBuilder.ApplicationServices here if you need to resolve things from the container.
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory) {
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(builder => {
            builder.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client..");
            builder.MapGrpcService<ChannelService>();
        });
    }
}
