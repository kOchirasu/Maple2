using System.CommandLine;
using Autofac;
using Autofac.Features.AttributeFilters;
using Maple2.Server.Commands;

namespace Maple2.Server.Modules;

internal class CliModule : Module {
    protected override void Load(ContainerBuilder builder) {
        builder.RegisterType<CommandRouter>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterAssemblyTypes(typeof(CommandRouter).Assembly)
            .PublicOnly()
            .WithAttributeFiltering()
            .Where(type => typeof(Command).IsAssignableFrom(type))
            .As<Command>()
            .SingleInstance();
    }
}