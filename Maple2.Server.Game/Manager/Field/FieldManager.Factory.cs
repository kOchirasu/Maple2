using System.Collections.Concurrent;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Manager.Field;

public partial class FieldManager {
    public class Factory {
        private readonly MapMetadataStorage mapStorage;
        private readonly MapEntityStorage entityStorage;
        private readonly ILogger logger;

        private readonly ConcurrentDictionary<(int MapId, int InstanceId), FieldManager> managers;

        public Factory(MapMetadataStorage mapStorage, MapEntityStorage entityStorage, ILogger<Factory> logger) {
            this.mapStorage = mapStorage;
            this.entityStorage = entityStorage;
            this.logger = logger;

            managers = new ConcurrentDictionary<(int, int), FieldManager>();
        }

        public FieldManager? Get(int mapId, int instanceId = 0) {
            if (!mapStorage.TryGet(mapId, out MapMetadata metadata)) {
                logger.LogError("Loading invalid Map:{MapId}", mapId);
                return null;
            }

            MapEntityMetadata entities = entityStorage.Get(metadata.XBlock);
            return managers.GetOrAdd((mapId, instanceId), new FieldManager(instanceId, metadata, entities));
        }
    }
}
