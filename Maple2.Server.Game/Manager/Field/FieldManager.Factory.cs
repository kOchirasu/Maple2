﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Autofac;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Serilog;

namespace Maple2.Server.Game.Manager.Field;

public partial class FieldManager {
    public sealed class Factory : IDisposable {
        #region Autofac Autowired
        // ReSharper disable MemberCanBePrivate.Global
        public required MapMetadataStorage MapMetadata { private get; init; }
        public required MapEntityStorage MapEntities { private get; init; }
        // ReSharper restore All
        #endregion

        private readonly ILogger logger = Log.Logger.ForContext<Factory>();

        private readonly IComponentContext context;
        private readonly ConcurrentDictionary<(int MapId, int InstanceId), FieldManager> fields;

        public Factory(IComponentContext context) {
            this.context = context;

            fields = new ConcurrentDictionary<(int, int), FieldManager>();
        }

        public FieldManager? Get(int mapId, int instanceId = 0) {
            var sw = new Stopwatch();
            sw.Start();
            if (!MapMetadata.TryGet(mapId, out MapMetadata? metadata)) {
                logger.Error("Loading invalid Map:{MapId}", mapId);
                return null;
            }

            if (!MapMetadata.TryGetUgc(mapId, out UgcMapMetadata? ugcMetadata)) {
                ugcMetadata = new UgcMapMetadata(mapId, new Dictionary<int, UgcMapGroup>());
            }

            // ReSharper disable once HeapView.CanAvoidClosure, defer instantiation unless it's needed.
            FieldManager field = fields.GetOrAdd((mapId, instanceId), _ => {
                MapEntityMetadata? entities = MapEntities.Get(metadata.XBlock);
                if (entities == null) {
                    throw new InvalidOperationException($"Failed to load entities for map: {mapId}");
                }

                var field = new FieldManager(metadata, ugcMetadata, entities);
                context.InjectProperties(field);
                field.Init();
                return field;
            });

            logger.Debug("Field:{MapId} Instance:{InstanceId} initialized in {Time}ms", mapId, instanceId, sw.ElapsedMilliseconds);
            return field;
        }

        public void Dispose() {
            foreach (FieldManager manager in fields.Values) {
                manager.Dispose();
            }

            fields.Clear();
        }
    }
}
