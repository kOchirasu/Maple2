using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public interface ITriggerObject : IByteSerializable {
    public int Id { get; }
    public bool Visible { get; }
}

public abstract class TriggerObject<T> : ITriggerObject where T : Trigger {
    public readonly T Metadata;

    public int Id => Metadata.TriggerId;
    public bool Visible { get; set; }

    public TriggerObject(T metadata) {
        Metadata = metadata;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteBool(Visible);
    }
}

public class TriggerObjectSound : TriggerObject<Ms2TriggerSound> {
    public TriggerObjectSound(Ms2TriggerSound metadata) : base(metadata) { }
}

public class TriggerObjectMesh : TriggerObject<Ms2TriggerMesh> {
    public bool MinimapVisible { get; init; }
    public int Fade { get; set; }
    public float Scale { get; set; } = 1f;

    public TriggerObjectMesh(Ms2TriggerMesh metadata) : base(metadata) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteBool(MinimapVisible);
        writer.WriteInt(Fade); // Fade 10 = 1s?
        writer.WriteUnicodeString();
        writer.WriteFloat(Scale);
    }
}

public class TriggerObjectActor : TriggerObject<Ms2TriggerActor> {
    public string SequenceName { get; set; } = string.Empty;

    public TriggerObjectActor(Ms2TriggerActor metadata) : base(metadata) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteUnicodeString(SequenceName);
    }
}

public class TriggerObjectRope : TriggerObject<Ms2TriggerRope> {
    public bool Animate { get; set; }
    public int Delay { get; set; }

    public TriggerObjectRope(Ms2TriggerRope metadata) : base(metadata) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteBool(Animate);
        writer.WriteInt(Delay);
    }
}

public class TriggerObjectLadder : TriggerObject<Ms2TriggerLadder> {
    public bool Animate { get; set; }
    public int Delay { get; set; }

    public TriggerObjectLadder(Ms2TriggerLadder metadata) : base(metadata) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteBool(Animate);
        writer.WriteInt(Delay);
    }
}

public class TriggerObjectEffect : TriggerObject<Ms2TriggerEffect> {
    public bool UnknownBool { get; set; }
    public int UnknownInt { get; set; }

    public TriggerObjectEffect(Ms2TriggerEffect metadata) : base(metadata) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteBool(UnknownBool);
        writer.WriteInt(UnknownInt);
    }
}

public class TriggerObjectCube : TriggerObject<Ms2TriggerCube> {
    public TriggerObjectCube(Ms2TriggerCube metadata) : base(metadata) { }
}

public class TriggerObjectCamera : TriggerObject<Ms2TriggerCamera> {
    public TriggerObjectCamera(Ms2TriggerCamera metadata) : base(metadata) { }
}
