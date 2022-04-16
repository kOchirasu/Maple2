using System.Numerics;
using Maple2.Model.Common;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Core.Data; 

public class ItemAppearance : IByteSerializable {
    public EquipColor Color;

    public ItemAppearance(EquipColor color) {
        this.Color = color;
    }

    public virtual ItemAppearance Clone() {
        return (ItemAppearance) this.MemberwiseClone();
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.Write<EquipColor>(Color);
    }

    public virtual void ReadFrom(IByteReader reader) {
        Color = reader.Read<EquipColor>();
    }
}

public class HairAppearance : ItemAppearance {
    public float BackLength { get; private set; }
    public Vector3 BackPosition1 { get; private set; }
    public Vector3 BackPosition2 { get; private set; }
    public float FrontLength { get; private set; }
    public Vector3 FrontPosition1 { get; private set; }
    public Vector3 FrontPosition2 { get; private set; }

    public HairAppearance(EquipColor color) : base(color) { }

    public override void WriteTo(IByteWriter writer) {
        writer.Write<EquipColor>(Color);
        writer.WriteFloat(BackLength);
        writer.Write<Vector3>(BackPosition1);
        writer.Write<Vector3>(BackPosition2);
        writer.WriteFloat(FrontLength);
        writer.Write<Vector3>(FrontPosition1);
        writer.Write<Vector3>(FrontPosition2);
    }

    public override void ReadFrom(IByteReader reader) {
        Color = reader.Read<EquipColor>();
        BackLength = reader.ReadFloat();
        BackPosition1 = reader.Read<Vector3>();
        BackPosition2 = reader.Read<Vector3>();
        FrontLength = reader.ReadFloat();
        FrontPosition1 = reader.Read<Vector3>();
        FrontPosition2 = reader.Read<Vector3>();
    }
}

public class DecalAppearance : ItemAppearance {
    public float Unknown1 { get; private set; }
    public float Unknown2 { get; private set; }
    public float Unknown3 { get; private set; }
    public float Unknown4 { get; private set; }

    public DecalAppearance(EquipColor color) : base(color) { }

    public override void WriteTo(IByteWriter writer) {
        writer.Write<EquipColor>(Color);
        writer.WriteFloat(Unknown1);
        writer.WriteFloat(Unknown2);
        writer.WriteFloat(Unknown3);
        writer.WriteFloat(Unknown4);
    }

    public override void ReadFrom(IByteReader reader) {
        Color = reader.Read<EquipColor>();
        Unknown1 = reader.ReadFloat();
        Unknown2 = reader.ReadFloat();
        Unknown3 = reader.ReadFloat();
        Unknown4 = reader.ReadFloat();
    }
}

public class CapAppearance : ItemAppearance {
    public Vector3 Position1 { get; private set; }
    public Vector3 Position2 { get; private set; }
    public Vector3 Position3 { get; private set; }
    public Vector3 Position4 { get; private set; }
    public float Unknown { get; private set; }

    public CapAppearance(EquipColor color) : base(color) { }

    public override void WriteTo(IByteWriter writer) {
        writer.Write<EquipColor>(Color);
        writer.Write<Vector3>(Position1);
        writer.Write<Vector3>(Position2);
        writer.Write<Vector3>(Position3);
        writer.Write<Vector3>(Position4);
        writer.WriteFloat(Unknown);
    }

    public override void ReadFrom(IByteReader reader) {
        Color = reader.Read<EquipColor>();
        Position1 = reader.Read<Vector3>();
        Position2 = reader.Read<Vector3>();
        Position3 = reader.Read<Vector3>();
        Position4 = reader.Read<Vector3>();
        Unknown = reader.ReadFloat();
    }
}
