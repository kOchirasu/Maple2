using Autofac;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Maple2.Server.Modules;

internal class NLogModule : Module {
    protected override void Load(ContainerBuilder builder) {
        builder.Register(_ => {
                var factory = new LoggerFactory();
                factory.AddProvider(new NLogLoggerProvider());
                return factory;
            })
            .As<ILoggerFactory>()
            .SingleInstance();
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>));
    }
}
