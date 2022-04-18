using System.Reflection;
using Autofac;
using Maple2.Database.Storage;
using Maple2.Server.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Module = Autofac.Module;

namespace Maple2.Server.Core.Modules; 

public class DataDbModule : Module {
    private const string NAME = "DataDbOptions";
    
    private readonly DbContextOptions options;
    
    public DataDbModule() {
        options = new DbContextOptionsBuilder()
            .UseMySql(Target.DATA_DB_CONNECTION, ServerVersion.AutoDetect(Target.DATA_DB_CONNECTION))
            .Options;
    }
    
    protected override void Load(ContainerBuilder builder) {
        builder.RegisterInstance(options)
            .Named<DbContextOptions>(NAME);
        
        builder.RegisterType<ItemMetadataStorage>()
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
