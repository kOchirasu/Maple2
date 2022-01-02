using System.IO;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Maple2.Server.Modules;

internal class NLogModule : Module {
    private const string APP_SETTINGS = "appsettings.json";

    static NLogModule() {
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(APP_SETTINGS, true, true)
            .Build();
        NLog.LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
    }

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
