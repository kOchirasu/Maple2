using System.CommandLine;
using Autofac;
using Maple2.Server.Commands;
using Maple2.Server.Servers.Game;
using Maple2.Server.Servers.Login;

namespace Maple2.Server.Config; 

internal static class Maple2ContainerConfig {
    public static IContainer Configure(LoginServer loginServer, GameServer gameServer) {
        var builder = new ContainerBuilder();

        builder.RegisterInstance(loginServer)
            .AsSelf();
        builder.RegisterInstance(gameServer)
            .AsSelf();

        builder.RegisterType<CommandRouter>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterAssemblyTypes(typeof(CommandRouter).Assembly)
            .PublicOnly()
            .Where(type => typeof(Command).IsAssignableFrom(type))
            .As<Command>()
            .SingleInstance();

        return builder.Build();
    }
}