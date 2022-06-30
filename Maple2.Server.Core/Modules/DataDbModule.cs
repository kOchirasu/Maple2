using System;
using Autofac;
using Maple2.Database.Context;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Module = Autofac.Module;

namespace Maple2.Server.Core.Modules;

public class DataDbModule : Module {
    private readonly DbContextOptions options;

    public DataDbModule() {
        string? dataDbConnection = Environment.GetEnvironmentVariable("DATA_DB_CONNECTION");
        if (dataDbConnection == null) {
            throw new ArgumentException("DATA_DB_CONNECTION environment variable was not set");
        }

        options = new DbContextOptionsBuilder()
            .UseMySql(dataDbConnection, ServerVersion.AutoDetect(dataDbConnection))
            .Options;
    }

    protected override void Load(ContainerBuilder builder) {
        // NoTracking, Metadata is cached separately, and we use a single context for lifetime.
        var context = new MetadataContext(options);
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        builder.RegisterInstance(context);

        builder.RegisterType<AnimationMetadata>().SingleInstance();
        builder.RegisterType<ItemMetadataStorage>().SingleInstance();
        builder.RegisterType<MagicPathMetadata>().SingleInstance();
        builder.RegisterType<MapMetadataStorage>().SingleInstance();
        builder.RegisterType<MapEntityStorage>().SingleInstance();
        builder.RegisterType<NpcMetadataStorage>().SingleInstance();
        builder.RegisterType<QuestMetadataStorage>().SingleInstance();
        builder.RegisterType<SkillMetadataStorage>().SingleInstance();
        builder.RegisterType<TableMetadataStorage>().SingleInstance();
        builder.RegisterType<UgcMapMetadata>().SingleInstance();
    }
}
