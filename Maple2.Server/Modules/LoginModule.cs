using Autofac;
using Maple2.Server.Network;
using Maple2.Server.PacketHandlers;
using Maple2.Server.Servers.Login;

namespace Maple2.Server.Modules;

internal class LoginModule : Module {
    protected override void Load(ContainerBuilder builder) {
        // Server
        builder.RegisterType<LoginServer>()
            .AsSelf()
            .SingleInstance();
        builder.RegisterType<PacketRouter<LoginSession>>()
            .As<PacketRouter<LoginSession>>()
            .SingleInstance();
        builder.RegisterType<LoginSession>()
            .AsSelf();

        // Make all packet handlers available to PacketRouter
        builder.RegisterAssemblyTypes(typeof(IPacketHandler<>).Assembly)
            .Where(type => typeof(IPacketHandler<LoginSession>).IsAssignableFrom(type))
            .As<IPacketHandler<LoginSession>>()
            .SingleInstance();
    }
}