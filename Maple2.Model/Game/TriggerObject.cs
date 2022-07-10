using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public abstract class TriggerObject : IByteSerializable {
    public int Id { get; init; }
    public bool Visible { get; init; }

    public TriggerObject(int id) {
        Id = id;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteBool(Visible);
    }
}

public class TriggerObjectSound : TriggerObject {
    public TriggerObjectSound(int id) : base(id) { }
}

public class TriggerObjectMesh : TriggerObject {
    public bool MinimapVisible { get; init; }
    public int Unknown { get; init; }

    public TriggerObjectMesh(int id) : base(id) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteBool(MinimapVisible);
        writer.WriteInt(Unknown); // Fade 10 = 1s?
        writer.WriteUnicodeString();
        writer.WriteFloat(1); // scale?
    }
}

public class TriggerObjectActor : TriggerObject {
    public string SequenceName { get; init; } = string.Empty;

    public TriggerObjectActor(int id) : base(id) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteUnicodeString(SequenceName);
    }
}

public class TriggerObjectRope : TriggerObject {
    public bool Animate { get; init; }
    public int Delay { get; init; }

    public TriggerObjectRope(int id) : base(id) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteBool(Animate);
        writer.WriteInt(Delay);
    }
}

public class TriggerObjectEffect : TriggerObject {
    public bool UnknownBool { get; init; }
    public int UnknownInt { get; init; }

    public TriggerObjectEffect(int id) : base(id) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteBool(UnknownBool);
        writer.WriteInt(UnknownInt);
    }
}

public class TriggerObjectCube : TriggerObject {
    public TriggerObjectCube(int id) : base(id) { }
}

public class TriggerObjectCamera : TriggerObject {
    public TriggerObjectCamera(int id) : base(id) { }
}
