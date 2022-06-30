using System;
using System.Net;
using Grpc.Net.ClientFactory;
using Maple2.Server.Core.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Maple2.Server.Core.Modules;

public class ChannelClientModule : GrpcClientModule {
    public override void Configure(IServiceCollection services) {
        services.AddGrpcClient<Channel.Service.Channel.ChannelClient>("1", Options);
    }

    protected override void Options(GrpcClientFactoryOptions options) {
        string channelService = Environment.GetEnvironmentVariable("CHANNEL_SERVICE") ?? IPAddress.Loopback.ToString();
        options.Address = new Uri($"http://{channelService}:{Target.GRPC_CHANNEL_PORT}");
    }
}
