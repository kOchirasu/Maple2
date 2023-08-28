using System;
using System.Reflection;
using Autofac;
using Maple2.Database.Storage;
using Microsoft.EntityFrameworkCore;
using Module = Autofac.Module;

namespace Maple2.Server.Core.Modules;

public class WebDbModule : Module {
    private const string NAME = "WebDbOptions";

    private readonly DbContextOptions options;

    public WebDbModule() {
        string? gameDbConnection = Environment.GetEnvironmentVariable("GAME_DB_CONNECTION");
        if (gameDbConnection == null) {
            throw new ArgumentException("GAME_DB_CONNECTION environment variable was not set");
        }

        options = new DbContextOptionsBuilder()
            .UseMySql(gameDbConnection, ServerVersion.AutoDetect(gameDbConnection))
            .Options;
    }

    protected override void Load(ContainerBuilder builder) {
        builder.RegisterInstance(options)
            .Named<DbContextOptions>(NAME);

        builder.RegisterType<WebStorage>()
            .WithParameter(Condition, Resolve)
            .SingleInstance();
    }

    private static bool Condition(ParameterInfo info, IComponentContext context) {
        return info.Name == "options";
    }

    private static DbContextOptions Resolve(ParameterInfo info, IComponentContext context) {
        return context.ResolveNamed<DbContextOptions>(NAME);
    }
}
