using Autofac;
using Maple2.Server.Network;
using Maple2.Server.PacketHandlers;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Maple2.Server.Servers.Game {
    internal static class GameContainerConfig {
        public static IContainer Configure() {
            var builder = new ContainerBuilder();

            // Logger
            builder.Register(_ => {
                    var factory = new LoggerFactory();
                    factory.AddProvider(new NLogLoggerProvider());
                    return factory;
                })
                .As<ILoggerFactory>()
                .SingleInstance();
            builder.RegisterGeneric(typeof(Logger<>))
                .As(typeof(ILogger<>))
                .SingleInstance();

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

            return builder.Build();
        }
    }
}