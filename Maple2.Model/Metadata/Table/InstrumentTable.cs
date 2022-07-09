using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record InstrumentTable(IReadOnlyDictionary<int, InstrumentMetadata> Entries) : Table(Discriminator.InstrumentTable);

public record InstrumentMetadata(int Id, int EquipId, int ScoreCount, int Category, int MidiId, int PercussionId);
