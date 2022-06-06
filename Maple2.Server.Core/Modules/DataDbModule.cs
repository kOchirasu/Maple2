using Autofac;
using Maple2.Database.Context;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Module = Autofac.Module;

namespace Maple2.Server.Core.Modules;

public class DataDbModule : Module {
    private readonly DbContextOptions options;

    public DataDbModule() {
        options = new DbContextOptionsBuilder()
            .UseMySql(Target.DATA_DB_CONNECTION, ServerVersion.AutoDetect(Target.DATA_DB_CONNECTION))
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
        builder.RegisterType<SkillMetadataStorage>().SingleInstance();
        builder.RegisterType<TableMetadataStorage>().SingleInstance();
        builder.RegisterType<UgcMapMetadata>().SingleInstance();
    }
}
