using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Common;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;
using Home = Maple2.Model.Game.Home;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IEnumerable<Plot> LoadPlotsForMap(int mapId) {
            return Context.UgcMap.Include(map => map.Cubes)
                .Where(map => map.MapId == mapId)
                .AsEnumerable()
                .Select(ToPlot)
                .ToList()!;
        }

        public PlotInfo? BuyPlot(long ownerId, PlotInfo plot, TimeSpan days) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            UgcMap? ugcMap = Context.UgcMap.SingleOrDefault(map => map.Id == plot.Id && !map.Indoor);
            if (ugcMap == null) {
                return null;
            }

            Debug.Assert(ugcMap.MapId == plot.MapId && ugcMap.Number == plot.Number && ugcMap.ApartmentNumber == plot.ApartmentNumber);
            if (ugcMap.OwnerId != 0 || ugcMap.ExpiryTime >= DateTime.Now) {
                return null;
            }

            ugcMap.OwnerId = ownerId;
            ugcMap.ExpiryTime = DateTime.UtcNow + days;
            Context.UgcMap.Update(ugcMap);
            Context.UgcMapCube.Where(cube => cube.UgcMapId == ugcMap.Id).Delete();

            return Context.TrySaveChanges() ? ToPlotInfo(ugcMap) : null;
        }

        public PlotInfo? ExtendPlot(PlotInfo plot, TimeSpan days) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            UgcMap? model = Context.UgcMap.Find(plot.Id);
            if (model == null) {
                return null;
            }

            Debug.Assert(model.MapId == plot.MapId && model.Number == plot.Number && model.ApartmentNumber == plot.ApartmentNumber);
            if (model.ExpiryTime < DateTime.Now) {
                return null;
            }

            model.ExpiryTime += days;
            Context.UgcMap.Update(model);

            return Context.TrySaveChanges() ? ToPlotInfo(model) : null;
        }

        public PlotInfo? ForfeitPlot(long ownerId, PlotInfo plot) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            UgcMap? model = Context.UgcMap.Find(plot.Id);
            if (model == null || model.OwnerId != ownerId) {
                return null;
            }

            Debug.Assert(model.MapId == plot.MapId && model.Number == plot.Number && model.ApartmentNumber == plot.ApartmentNumber);
            if (model.ExpiryTime < DateTime.Now) {
                return null;
            }

            model.OwnerId = 0;
            model.ExpiryTime = DateTime.UtcNow;
            Context.UgcMapCube.Where(cube => cube.UgcMapId == model.Id).Delete();
            Context.UgcMap.Update(model);

            return Context.TrySaveChanges() ? ToPlotInfo(model) : null;
        }

        public bool SaveHome(Home home) {
            Model.Home model = home!;
            Context.Home.Update(model);
            if (!Context.TrySaveChanges()) {
                return false;
            }

            home.LastModified = model.LastModified.ToEpochSeconds();
            return true;
        }

        public bool SavePlot(params PlotInfo[] plotInfos) {
            foreach (PlotInfo plotInfo in plotInfos) {
                UgcMap? model = Context.UgcMap.Find(plotInfo.Id);
                if (model == null) {
                    return false;
                }

                model.OwnerId = plotInfo.OwnerId;
                model.MapId = plotInfo.MapId;
                model.Number = plotInfo.Number;
                model.ApartmentNumber = plotInfo.ApartmentNumber;
                model.ExpiryTime = plotInfo.ExpiryTime.FromEpochSeconds();
                Context.UgcMap.Update(model);
            }

            return Context.TrySaveChanges();
        }

        public bool InitUgcMap(IEnumerable<UgcMapMetadata> maps) {
            foreach (UgcMapMetadata map in maps) {
                if (map.Id == Constant.DefaultHomeMapId) {
                    continue;
                }

                foreach (UgcMapGroup group in map.Plots.Values) {
                    Context.UgcMap.Add(new UgcMap {
                        MapId = map.Id,
                        Number = group.Number,
                        ApartmentNumber = group.ApartmentNumber,
                    });
                }
            }

            return Context.TrySaveChanges();
        }

        private Plot? ToPlot(UgcMap? ugcMap) {
            if (ugcMap == null || !game.mapMetadata.TryGetUgc(ugcMap.MapId, out UgcMapMetadata? metadata)) {
                return null;
            }

            if (!metadata.Plots.TryGetValue(ugcMap.Number, out UgcMapGroup? group)) {
                return null;
            }

            var plot = new Plot(group) {
                Id = ugcMap.Id,
                OwnerId = ugcMap.OwnerId,
                MapId = ugcMap.MapId,
                Number = ugcMap.Number,
                ApartmentNumber = 0,
                ExpiryTime = ugcMap.ExpiryTime.ToEpochSeconds(),
            };

            foreach ((UgcItemCube cube, Vector3B position, float rotation) entry in ugcMap.Cubes) {
                plot.Cubes.Add(entry.position, (entry.cube, entry.rotation));
            }

            return plot;
        }

        private PlotInfo? ToPlotInfo(UgcMap? ugcMap) {
            if (ugcMap == null || !game.mapMetadata.TryGetUgc(ugcMap.MapId, out UgcMapMetadata? metadata)) {
                return null;
            }

            if (!metadata.Plots.TryGetValue(ugcMap.Number, out UgcMapGroup? group)) {
                return null;
            }

            return new PlotInfo(group) {
                Id = ugcMap.Id,
                OwnerId = ugcMap.OwnerId,
                MapId = ugcMap.MapId,
                Number = ugcMap.Number,
                ApartmentNumber = 0,
                ExpiryTime = ugcMap.ExpiryTime.ToEpochSeconds(),
            };
        }
    }
}
