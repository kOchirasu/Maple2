using Autofac;
using Maple2.Server.Network;
using Maple2.Server.PacketHandlers;
using Maple2.Server.Servers.Game;

namespace Maple2.Server.Modules;

internal class GameModule : Module {
    protected override void Load(ContainerBuilder builder) {
        // Server
        builder.RegisterType<GameServer>()
            .AsSelf()
            .SingleInstance();
        builder.RegisterType<PacketRouter<GameSession>>()
            .As<PacketRouter<GameSession>>()
            .SingleInstance();
        builder.RegisterType<GameSession>()
            .AsSelf();

        // Make all packet handlers available to PacketRouter
        builder.RegisterAssemblyTypes(typeof(IPacketHandler<>).Assembly)
            .Where(type => typeof(IPacketHandler<GameSession>).IsAssignableFrom(type))
            .As<IPacketHandler<GameSession>>()
            .SingleInstance();
    }
}