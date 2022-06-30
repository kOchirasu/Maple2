using System.Collections.Concurrent;
using Autofac;
using Maple2.Server.Core.Modules;
using Maple2.Server.Global.Service;
using Maple2.Server.World.Containers;
using Maple2.Server.World.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.World;

public class Startup {
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) {
        this.Configuration = configuration;
    }

    // ConfigureServices is where you register dependencies. This gets called by the runtime before the
    // ConfigureContainer method, below.
    public void ConfigureServices(IServiceCollection services) {
        services.AddGrpc();
        services.RegisterModule<ChannelClientModule>();
        services.AddMemoryCache();
    }

    // ConfigureContainer is where you can register things directly with Autofac. This runs after
    // ConfigureServices so the things here will override registrations made in ConfigureServices.
    // Don't build the container; that gets done for you by the factory.
    public void ConfigureContainer(ContainerBuilder builder) {
        // Database
        builder.RegisterModule<GameDbModule>();
        builder.RegisterModule<DataDbModule>();

        builder.RegisterType<PlayerChannelLookup>()
            .SingleInstance();
    }

    // Configure is where you add middleware. This is called after ConfigureContainer. You can use
    // IApplicationBuilder.ApplicationServices here if you need to resolve things from the container.
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory) {
        app.UseRouting();
        app.UseEndpoints(builder => {
            builder.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
            builder.MapGrpcService<WorldService>();
            builder.MapGrpcService<GlobalService>();
        });
    }
}
