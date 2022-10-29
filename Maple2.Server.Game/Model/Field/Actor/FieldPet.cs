using System;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Model;

public sealed class FieldPet : FieldNpc {
    private readonly FieldPlayer? owner;
    public readonly Item Pet;

    public int OwnerId => owner?.ObjectId ?? 0;
    public int SkinId { get; private set; }

    public readonly float Scale = 1f;

    private IActor? capturer;
    public short TamingPoint = 0;

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

    public override void ApplyDamage(IActor caster, DamageRecord damage, SkillMetadataAttack attack) {
        if (attack.Pet == null) {
            return;
        }

        var targetRecord = new DamageRecordTarget {
            ObjectId = ObjectId,
        };
        long damageAmount = TamingPoint - Math.Min(TamingPoint + attack.Pet.TamingPoint, Constant.TamingPetMaxPoint);
        TamingPoint -= (short) damageAmount;
        targetRecord.AddDamage(damageAmount == 0 ? DamageType.Miss : DamageType.Normal, damageAmount);
        if (damageAmount != 0) {
            Field.Broadcast(PetPacket.SyncTaming(caster.ObjectId, this));
        }

        if (attack.Pet.TrapLevel > 0) {
            if (attack.Pet.ForcedTaming) {
                capturer = caster;
                Stats[StatAttribute.Health].Current = 0;
            } else if (TamingPoint >= Constant.TamingPetMaxPoint) { // trap has chance to fail
                capturer = caster;
                Stats[StatAttribute.Health].Current = 0;
            }
        }

        damage.Targets.Add(targetRecord);
    }

    protected override ByteWriter Control() => NpcControlPacket.ControlPet(this);

    protected override void Remove() => Field.RemovePet(ObjectId);

    public void UpdateSkin(int skinId) {
        SkinId = skinId > 0 ? skinId : Value.Id;
    }

    protected override void OnDeath() {
        base.OnDeath();

        if (capturer is FieldPlayer player) {
            FieldItem fieldItem = Field.SpawnItem(this, Pet);
            player.Session.Send(FieldPacket.DropItem(fieldItem));
        }
    }
}
