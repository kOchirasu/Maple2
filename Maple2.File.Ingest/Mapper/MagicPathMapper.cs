using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Table;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class MagicPathMapper : TypeMapper<MagicPathMetadata> {
    private readonly MagicPathParser parser;
    private readonly HashSet<long> addedIds = new();

    public MagicPathMapper(M2dReader xmlReader) {
        parser = new MagicPathParser(xmlReader);
    }

    protected override IEnumerable<MagicPathMetadata> Map() {
        addedIds.Clear();

        foreach (MagicPath magicPath in parser.Parse()) {
            foreach (MagicType type in magicPath.type) {
                // Dropping duplicates for now (60073021, 50000303, 5009, 5101)
                if (addedIds.Contains(type.id)) {
                    continue;
                }
                addedIds.Add(type.id);

                List<MagicPathMetadataMove> moves = type.move.Select(move => new MagicPathMetadataMove(
                    Align: move.align,
                    AlignHeight: move.alignCubeHeight,
                    Rotate: move.rotation,
                    IgnoreAdjust: move.ignoreAdjustCubePosition,
                    Direction: move.direction,
                    FireOffset: move.fireOffsetPosition,
                    FireFixed: move.fireFixedPosition,
                    Velocity: move.vel,
                    Distance: move.distance,
                    RotateZDegree: move.dirRotZDegree,
                    LifeTime: move.lifeTime,
                    DelayTime: move.delayTime,
                    SpawnTime: move.spawnTime,
                    DestroyTime: move.destroyTime
                )).ToList();
                yield return new MagicPathMetadata(type.id, moves);
            }
        }
    }
}
