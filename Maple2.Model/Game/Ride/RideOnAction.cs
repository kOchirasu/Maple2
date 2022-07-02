using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class RideOnAction : IByteSerializable {
    public readonly RideOnType Type;
    public readonly int RideId;
    public readonly int ObjectId;

    public RideOnAction(int rideId, int objectId) : this(RideOnType.Default, rideId, objectId) { }

    protected RideOnAction(RideOnType type, int rideId, int objectId) {
        Type = type;
        RideId = rideId;
        ObjectId = objectId;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.Write<RideOnType>(Type);
        writer.WriteInt(RideId);
        writer.WriteInt(ObjectId);
    }
}

public class RideOnActionUseItem : RideOnAction {
    public readonly int ItemId; // Same as MountId?
    public readonly long ItemUid;

    public RideOnActionUseItem(int rideId, int objectId, int itemId, long itemUid) : base(RideOnType.UseItem, rideId, objectId) {
        ItemId = itemId;
        ItemUid = itemUid;
    }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt(ItemId);
        writer.WriteLong(ItemUid);
    }
}

public class RideOnActionAdditionalEffect : RideOnAction {
    public readonly int SkillId; // Same as MountId?
    public readonly short SkillLevel;

    public RideOnActionAdditionalEffect(int rideId, int objectId, int skillId, short skillLevel) : base(RideOnType.AdditionalEffect, rideId, objectId) {
        SkillId = skillId;
        SkillLevel = skillLevel;
    }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt(SkillId);
        writer.WriteShort(SkillLevel);
    }
}

public class RideOnActionHideAndSeek : RideOnAction {
    public RideOnActionHideAndSeek(int rideId, int objectId) : base(RideOnType.HideAndSeek, rideId, objectId) { }
}
