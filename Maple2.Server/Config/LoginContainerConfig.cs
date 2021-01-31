using Autofac;
using Maple2.Server.Network;
using Maple2.Server.PacketHandlers;
using Maple2.Server.Servers.Login;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Maple2.Server.Config {
    internal static class LoginContainerConfig {
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

            return builder.Build();
        }
    }
}