using System;
using System.Net.Http;
using Grpc.Net.ClientFactory;
using Maple2.Server.Core.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Maple2.Server.Core.Modules;

public class WorldClientModule : GrpcClientModule {
    public override void Configure(IServiceCollection services) {
        services.AddGrpcClient<World.Service.World.WorldClient>(Options);
        services.AddGrpcClient<Global.Service.Global.GlobalClient>(Options);
    }

    protected override void Options(GrpcClientFactoryOptions options) {
        options.Address = new Uri($"http://{Target.GRPC_WORLD_IP}:{Target.GRPC_WORLD_PORT}");
    }
}
