using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Model;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Plot = Maple2.Model.Game.Plot;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Plot? GetPlot(long id) {
            Model.Plot? model = Context.Plot.Find(id);
            return ToPlot(model);
        }

        public IEnumerable<Plot> LoadPlotsForMap(int mapId) {
            var results = (from plot in Context.Plot where plot.MapId == mapId
                join plotCubes in Context.PlotCubes on plot.Id equals plotCubes.Id into grouping
                from plotCubes in grouping.DefaultIfEmpty()
                select new {plot, plotCubes}).ToList();

            foreach (var result in results) {
                if (result == null) {
                    continue;
                }

                Plot plot = ToPlot(result.plot)!;
                if (result.plotCubes != null) {
                    foreach (PlacedCube cube in result.plotCubes.Cubes) {
                        UgcItemCube itemCube = cube.HasTemplate
                            ? new UgcItemCube(cube.ItemId, cube.ItemUid, GetTemplate(cube.ItemUid))
                            : new UgcItemCube(cube.ItemId, cube.ItemUid);
                        plot.Cubes.Add(cube.Position, (itemCube, cube.Rotation));
                    }
                }

                yield return plot;
            }
        }

        public Plot? CreatePlot(Plot plot, long ownerId) {
            Model.Plot model = plot!;
            model.Id = 0;
            model.OwnerId = ownerId;

            Context.Plot.Add(plot!);
            if (!Context.TrySaveChanges()) {
                return null;
            }

            // We don't reconvert to avoid looking up cubes.
            plot.Uid = model.Id;
            plot.OwnerId = ownerId;
            return plot;
        }

        public bool UpdatePlot(Plot plot) {
            Context.Plot.Update(plot!);
            return Context.TrySaveChanges();
        }

        private Plot? ToPlot(Model.Plot? model) {
            if (model == null || !game.mapMetadata.TryGetUgc(model.MapId, out UgcMapMetadata? metadata)) {
                return null;
            }

            UgcMapGroup? group = metadata.Groups.FirstOrDefault(group => group.GroupId == model.Number);
            if (group == null) {
                return null;
            }

            return model.Convert(group);
        }
    }
}
