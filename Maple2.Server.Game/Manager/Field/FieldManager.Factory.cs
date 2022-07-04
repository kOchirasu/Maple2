using System;
using System.Collections.Concurrent;
using System.Threading;
using Autofac;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Serilog;
using Serilog.Core;

namespace Maple2.Server.Game.Manager.Field;

public partial class FieldManager {
    public sealed class Factory : IDisposable {
        #region Autofac Autowired
        // ReSharper disable MemberCanBePrivate.Global
        public MapMetadataStorage MapMetadata { private get; init; } = null!;
        public MapEntityStorage MapEntities { private get; init; } = null!;
        // ReSharper restore All
        #endregion

        private readonly ILogger logger = Log.Logger.ForContext<Factory>();

        private readonly IComponentContext context;
        private readonly ConcurrentDictionary<(int MapId, int InstanceId), FieldManager> managers;

        public Factory(IComponentContext context) {
            this.context = context;

            managers = new ConcurrentDictionary<(int, int), FieldManager>();
        }

        public FieldManager? Get(int mapId, int instanceId = 0) {
            if (!MapMetadata.TryGet(mapId, out MapMetadata? metadata)) {
                logger.Error("Loading invalid Map:{MapId}", mapId);
                return null;
            }

            // ReSharper disable once HeapView.CanAvoidClosure, defer instantiation unless it's needed.
            return managers.GetOrAdd((mapId, instanceId), _ => {
                MapEntityMetadata? entities = MapEntities.Get(metadata.XBlock);
                if (entities == null) {
                    throw new InvalidOperationException($"Failed to load entities for map: {mapId}");
                }

                var field = new FieldManager(instanceId, metadata, entities);
                context.InjectProperties(field);
                field.Init();
                return field;
            });
        }

        public void Dispose() {
            foreach (FieldManager manager in managers.Values) {
                manager.Dispose();
            }

            managers.Clear();
        }
    }
}
