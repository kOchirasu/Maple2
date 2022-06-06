using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class AnimationMapper : TypeMapper<AnimationMetadata> {
    private readonly AniKeyTextParser parser;

    public AnimationMapper(M2dReader xmlReader) {
        parser = new AniKeyTextParser(xmlReader);
    }

    protected override IEnumerable<AnimationMetadata> Map() {
        foreach (AnimationData data in parser.Parse()) {
            foreach (KeyFrameMotion kfm in data.kfm) {
                IEnumerable<(int Id, AnimationSequence Sequence)> sequences = kfm.seq.Select(sequence => (
                    sequence.id,
                    new AnimationSequence(
                        Name: sequence.name,
                        Time: (float) (sequence.key.FirstOrDefault(key => key.name == "end")?.time ?? default))
                ));
                yield return new AnimationMetadata(
                    kfm.name,
                    sequences.ToDictionary(entry => entry.Id, entry => entry.Sequence));
            }
        }
    }
}
