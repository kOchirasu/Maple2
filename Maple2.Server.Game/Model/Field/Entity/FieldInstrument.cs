using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;

namespace Maple2.Server.Game.Model;

public class FieldInstrument : FieldEntity<InstrumentMetadata> {
    public int OwnerId { get; init; }
    public bool Improvising { get; set; }
    public int StartTick { get; set; }

    public FieldInstrument(FieldManager field, int objectId, InstrumentMetadata value) : base(field, objectId, value) { }
}
