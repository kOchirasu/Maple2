using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class RideOffAction : IByteSerializable {
    private readonly RideOffType type;
    private readonly bool forced;

    public RideOffAction(bool forced) : this(RideOffType.Default) {
        this.forced = forced;
    }

    protected RideOffAction(RideOffType type) {
        this.type = type;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.Write<RideOffType>(type);
        writer.WriteBool(forced);
    }
}

public class RideOffActionUseSkill : RideOffAction {
    public RideOffActionUseSkill() : base(RideOffType.UseSkill) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt(); // RideOffAction+28
        writer.WriteByte(); // RideOffAction+32
        writer.WriteInt(); // RideOffAction+36
    }
}

public class RideOffActionInteract : RideOffAction {
    public RideOffActionInteract() : base(RideOffType.Interact) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt(); // RideOffAction+28
        writer.WriteUnicodeString();
    }
}

public class RideOffActionTaxi : RideOffAction {
    public RideOffActionTaxi() : base(RideOffType.Taxi) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt(); // RideOffAction+28
    }
}

public class RideOffActionCashCall : RideOffAction {
    public RideOffActionCashCall() : base(RideOffType.CashCall) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt();  // RideOffAction+28
        writer.WriteByte(); // RideOffAction+32
        writer.WriteLong();  // RideOffAction+20
    }
}

public class RideOffActionBeautyShop : RideOffAction {
    public RideOffActionBeautyShop() : base(RideOffType.BeautyShop) { }
}

public class RideOffActionTakeLr : RideOffAction {
    public RideOffActionTakeLr() : base(RideOffType.TakeLr) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt();  // RideOffAction+28
        writer.WriteByte(); // RideOffAction+32
    }
}

public class RideOffActionHold : RideOffAction {
    public RideOffActionHold() : base(RideOffType.Hold) { }
}

public class RideOffActionRecall : RideOffAction {
    public RideOffActionRecall() : base(RideOffType.Recall) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteLong(); // RideOffAction+16
    }
}

public class RideOffActionSummonPetOn : RideOffAction {
    public RideOffActionSummonPetOn() : base(RideOffType.SummonPetOn) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteLong(); // RideOffAction+16
        writer.WriteByte(); // RideOffAction+40
    }
}

public class RideOffActionSummonPetTransfer : RideOffAction {
    public RideOffActionSummonPetTransfer() : base(RideOffType.SummonPetTransfer) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteLong(); // RideOffAction+16
    }
}

public class RideOffActionHomeConvenient : RideOffAction {
    public RideOffActionHomeConvenient() : base(RideOffType.HomeConvenient) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteByte(); // RideOffAction+28
    }
}

public class RideOffActionDisableField : RideOffAction {
    public RideOffActionDisableField() : base(RideOffType.DisableField) { }
}

public class RideOffActionDead : RideOffAction {
    public RideOffActionDead() : base(RideOffType.Dead) { }
}

public class RideOffActionAdditionalEffect : RideOffAction {
    public RideOffActionAdditionalEffect() : base(RideOffType.AdditionalEffect) { }
}

public class RideOffActionRidingUi : RideOffAction {
    public RideOffActionRidingUi() : base(RideOffType.RidingUi) { }
}

public class RideOffActionHomemade : RideOffAction {
    public RideOffActionHomemade() : base(RideOffType.Homemade) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteUnicodeString();
        writer.WriteInt(); // RideOffAction+32
        writer.WriteShort(); // RideOffAction+36
        writer.WriteInt(); // RideOffAction+40
    }
}

public class RideOffActionAutoInteraction : RideOffAction {
    public RideOffActionAutoInteraction() : base(RideOffType.AutoInteraction) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt();  // RideOffAction+28
        writer.WriteByte(); // RideOffAction+32
        writer.WriteShort(); // RideOffAction+36
    }
}

public class RideOffActionAutoClimb : RideOffAction {
    public RideOffActionAutoClimb() : base(RideOffType.AutoClimb) { }
}

public class RideOffActionCoupleEmotion : RideOffAction {
    public RideOffActionCoupleEmotion() : base(RideOffType.CoupleEmotion) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteInt();  // RideOffAction+28
        writer.WriteLong(); // RideOffAction+16
    }
}

// This can't be used for RideOff?
//
// public class RideOffActionReact : RideOffAction {
//     public RideOffActionReact() : base(RideOffType.React) { }
//
//     public override void WriteTo(IByteWriter writer) {
//         base.WriteTo(writer);
//         writer.WriteInt();  // RideOffAction+28
//     }
// }

public class RideOffActionUseFunctionItem : RideOffAction {
    public RideOffActionUseFunctionItem() : base(RideOffType.UseFunctionItem) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteLong();  // RideOffAction+16
        writer.WriteUnicodeString();
    }
}

public class RideOffActionNurturing : RideOffAction {
    public RideOffActionNurturing() : base(RideOffType.Nurturing) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteUnicodeString();
        writer.WriteInt(); // RideOffAction+32
    }
}

public class RideOffActionGroggy : RideOffAction {
    public RideOffActionGroggy() : base(RideOffType.Groggy) { }
}

public class RideOffActionUnRideSkill : RideOffAction {
    public RideOffActionUnRideSkill() : base(RideOffType.UnRideSkill) { }
}

public class RideOffActionUseGlideItem : RideOffAction {
    public RideOffActionUseGlideItem() : base(RideOffType.UseGlideItem) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteLong(); // RideOffAction+16
        writer.WriteByte(); // RideOffAction+40
        writer.WriteInt(); // RideOffAction+44
    }
}

public class RideOffActionHideAndSeek : RideOffAction {
    public RideOffActionHideAndSeek() : base(RideOffType.HideAndSeek) { }
}
