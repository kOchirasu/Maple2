using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public sealed class FieldPet : FieldNpc {
    private readonly FieldPlayer? owner;
    public readonly Item Pet;

    public int OwnerId => owner?.ObjectId ?? 0;
    public int SkinId { get; private set; }

    public readonly float Scale = 1f;
    public readonly short TamingLevel = 0;

    public FieldPet(FieldManager field, int objectId, Npc npc, Item pet, FieldPlayer? owner = null) : base(field, objectId, npc) {
        this.owner = owner;
        Pet = pet;

        // Use Badge for SkinId if equipped.
        if (owner != null && owner.Session.Item.Equips.Badge.TryGetValue(BadgeType.PetSkin, out Item? value)) {
            SkinId = value.Badge?.PetSkinId ?? Value.Id;
        } else {
            SkinId = Value.Id;
        }
    }

    protected override ByteWriter Control() => NpcControlPacket.ControlPet(this);

    public void UpdateSkin(int skinId) {
        SkinId = skinId > 0 ? skinId : Value.Id;
    }
}
