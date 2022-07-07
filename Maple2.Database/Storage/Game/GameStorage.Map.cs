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
using Plot = Maple2.Model.Game.Plot;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Plot? GetPlot(long id) {
            UgcMap? model = Context.UgcMap.Find(id);
            return ToPlot(model, layout: null);
        }

        public IEnumerable<Plot> LoadPlotsForMap(int mapId) {
            var results = (from plot in Context.UgcMap where plot.MapId == mapId
                join plotCubes in Context.UgcMapLayout on plot.Id equals plotCubes.Id into grouping
                from plotCubes in grouping.DefaultIfEmpty()
                select new {plot, plotCubes}).ToList();

            foreach (var result in results) {
                if (result == null) {
                    continue;
                }

                yield return ToPlot(result.plot, result.plotCubes)!;
            }
        }

        public Plot? BuyPlot(long ownerId, Plot plot, TimeSpan days) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Home? home = Context.Home.Find(ownerId);
            if (home == null) {
                return null;
            }

            home.Plot = Context.UgcMap.Find(plot.Id);
            if (home.Plot == null) {
                return null;
            }

            Debug.Assert(home.Plot.MapId == plot.MapId && home.Plot.Number == plot.Number && home.Plot.ApartmentNumber == plot.ApartmentNumber);
            if (home.Plot.OwnerId != 0 || home.Plot.ExpiryTime >= DateTime.Now) {
                return null;
            }

            home.Plot.OwnerId = ownerId;
            home.Plot.ExpiryTime = DateTime.UtcNow + days;
            Context.Home.Update(home);

            UgcMapLayout? layout = Context.UgcMapLayout.Find(home.Plot.Id);
            if (layout != null) {
                Context.UgcMapLayout.Remove(layout);
            }

            return Context.TrySaveChanges() ? ToPlot(home.Plot, layout: null) : null;
        }

        public bool ExtendPlot(Plot plot, TimeSpan days) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            UgcMap? model = Context.UgcMap.Find(plot.Id);
            if (model == null) {
                return false;
            }

            Debug.Assert(model.MapId == plot.MapId && model.Number == plot.Number && model.ApartmentNumber == plot.ApartmentNumber);
            if (model.ExpiryTime < DateTime.Now) {
                return false;
            }

            model.ExpiryTime += days;
            Context.UgcMap.Update(model);

            return Context.TrySaveChanges();
        }

        public Plot? ForfeitPlot(long ownerId, Plot plot) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            Model.Home? home = Context.Home.Find(ownerId);
            if (home == null) {
                return null;
            }

            home.PlotId = null;
            Context.Home.Update(home);

            UgcMap? model = Context.UgcMap.Find(plot.Id);
            if (model == null) {
                return null;
            }

            Debug.Assert(model.MapId == plot.MapId && model.Number == plot.Number && model.ApartmentNumber == plot.ApartmentNumber);
            if (model.ExpiryTime < DateTime.Now) {
                return null;
            }

            model.OwnerId = 0;
            model.ExpiryTime = DateTime.UtcNow.AddDays(3);
            Context.UgcMap.Update(model);

            UgcMapLayout? layout = Context.UgcMapLayout.Find(model.Id);
            if (layout != null) {
                Context.UgcMapLayout.Remove(layout);
            }

            return Context.TrySaveChanges() ? ToPlot(model, layout: null) : null;
        }

        public bool InitUgcMap(IEnumerable<UgcMapMetadata> maps) {
            foreach (UgcMapMetadata map in maps) {
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

        private Plot? ToPlot(UgcMap? ugcMap, UgcMapLayout? layout) {
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

                Origin = layout?.Origin ?? default,
                // TODO: Area here is X*Y instead of X/Y individually
                Dimensions = layout?.Origin ?? new Vector3B((sbyte) group.Limit.Area, (sbyte) group.Limit.Area, (sbyte) group.Limit.Height),
                LastModified = layout?.LastModified.ToEpochSeconds() ?? 0,
            };

            if (layout != null) {
                foreach (Cube cube in layout.Cubes) {
                    UgcItemCube itemCube = cube.HasTemplate
                        ? new UgcItemCube(cube.ItemId, cube.ItemUid, GetTemplate(cube.ItemUid))
                        : new UgcItemCube(cube.ItemId, cube.ItemUid);
                    plot.Cubes.Add(cube.Position, (itemCube, cube.Rotation));
                }
            }

            return plot;
        }
    }
}
