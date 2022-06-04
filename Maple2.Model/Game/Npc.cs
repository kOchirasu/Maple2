using Maple2.Model.Metadata;

namespace Maple2.Model.Game;

public class Npc {
    public readonly NpcMetadata Metadata;

    public int Id => Metadata.Id;

    public Npc(NpcMetadata metadata) {
        Metadata = metadata;
    }
}
