using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;

namespace Maple2.Server.Game.Model;

public class FieldEffect : ActorBase<AdditionalEffectMetadata> {
    public FieldEffect(FieldManager field, int objectId, AdditionalEffectMetadata value) : base(field, objectId, value) { }


}
