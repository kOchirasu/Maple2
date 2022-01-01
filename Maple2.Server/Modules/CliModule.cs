using System.CommandLine;
using Autofac;
using Maple2.Server.Commands;

namespace Maple2.Server.Modules;

internal class CliModule : Module {
    protected override void Load(ContainerBuilder builder) {
        builder.RegisterType<CommandRouter>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterAssemblyTypes(typeof(CommandRouter).Assembly)
            .PublicOnly()
            .Where(type => typeof(Command).IsAssignableFrom(type))
            .As<Command>()
            .SingleInstance();
    }
}