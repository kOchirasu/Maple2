using Maple2.Database.Context;
using Maple2.File.Flat;
using Maple2.File.Flat.maplestory2library;
using Maple2.File.IO;
using Maple2.File.Parser.Flat;
using Maple2.File.Parser.MapXBlock;
using Maple2.Model.Metadata;
using static M2dXmlGenerator.FeatureLocaleFilter;

namespace Maple2.File.Ingest.Mapper;

public class MapEntityMapper : TypeMapper<MapEntity> {
    private readonly HashSet<string> xBlocks;
    private readonly XBlockParser parser;

    public MapEntityMapper(MetadataContext db, M2dReader exportedReader) {
        xBlocks = db.MapMetadata.Select(metadata => metadata.XBlock).ToHashSet();
        var index = new FlatTypeIndex(exportedReader);
        parser = new XBlockParser(exportedReader, index);
    }

    private IEnumerable<MapEntity> ParseMap(string xblock, IEnumerable<IMapEntity> entities) {
        foreach (IMapEntity entity in entities) {
            switch (entity) {
                case IPortal portal:
                    if (!FeatureEnabled(portal.feature) || !HasLocale(portal.locale)) continue;
                    yield return new MapEntity(xblock, new Guid(entity.EntityId), entity.EntityName) {
                        Block = new Portal(portal.PortalID, portal.TargetFieldSN, portal.TargetPortalID,
                            portal.PortalType, portal.ActionType, portal.Position, portal.Rotation,
                            portal.PortalDimension, portal.PortalOffset, portal.MinimapIconVisible, portal.PortalEnable)
                    };
                    continue;
                case ISpawnPoint spawn:
                    switch (spawn) {
                        case ISpawnPointPC pcSpawn:
                            yield return new MapEntity(xblock, new Guid(entity.EntityId), entity.EntityName) {
                                Block = new SpawnPointPC(pcSpawn.SpawnPointID, pcSpawn.Position, pcSpawn.Rotation,
                                    pcSpawn.IsVisible, pcSpawn.Enable)
                            };
                            continue;
                    }
                    continue;
            }
        }
    }

    protected override IEnumerable<MapEntity> Map() {
        IEnumerable<MapEntity> results = Enumerable.Empty<MapEntity>();
        parser.Parse((xblock, entities) => {
            xblock = xblock.ToLower();
            if (!xBlocks.Contains(xblock)) {
                return;
            }

            results = results.Concat(ParseMap(xblock, entities));
        });

        return results;
    }
}
