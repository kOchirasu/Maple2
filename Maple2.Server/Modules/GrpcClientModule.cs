using System;
using System.Net.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Maple2.Server.Servers.World.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Maple2.Server.Modules;

internal class GrpcClientModule : Module {
    protected override void Load(ContainerBuilder builder) {
        var services = new ServiceCollection();

        services.AddGrpcClient<Greeter.GreeterClient>(options => {
            options.Address = new Uri("https://localhost:5001");
            options.ChannelOptionsActions.Add(options => {
                // Return "true" to allow certificates that are untrusted/invalid
                options.HttpHandler = new HttpClientHandler {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
            });
        });

        builder.Populate(services);
    }
}
