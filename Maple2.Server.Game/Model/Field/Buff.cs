using System;
using System.Numerics;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Server.Game.Model;

public class Buff : IFieldObject, IByteSerializable {
    public int ObjectId { get; init; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public readonly IActor Caster;
    public readonly IActor Target;

    public readonly AdditionalEffectMetadata Metadata;
    public int Id => Metadata.Id;
    public short Level => Metadata.Level;

    public int StartTick { get; private set; }
    public int EndTick { get; private set; }
    public int NextTick { get; private set; }
    public int Stacks { get; private set; }

    public bool Enabled => Environment.TickCount <= EndTick;

    public Buff(int objectId, AdditionalEffectMetadata metadata, IActor caster, IActor target) {
        ObjectId = objectId;
        Metadata = metadata;
        Caster = caster;
        Target = target;

        // Initialize
        Stack();
        NextTick = StartTick + Metadata.Property.DelayTick + Metadata.Property.IntervalTick;
    }

    public void Stack() {
        Stacks = Math.Min(Stacks + 1, Metadata.Property.MaxCount);
        StartTick = Environment.TickCount;
        EndTick = StartTick + Metadata.Property.DurationTick;
    }

    public bool ShouldProc() {
        if (!Enabled || Environment.TickCount < NextTick) {
            return false;
        }

        // Buffs with IntervalTick=0 will just proc a single time
        if (Metadata.Property.IntervalTick == 0) {
            NextTick = EndTick + 1;
            return true;
        }

        NextTick += Metadata.Property.IntervalTick;
        return true;
    }

    public void WriteTo(IByteWriter writer) {
        WriteAdditionalEffect(writer);
        WriteAdditionalEffect2(writer);
    }

    // AdditionalEffect
    public void WriteAdditionalEffect(IByteWriter writer) {
        writer.WriteInt(StartTick);
        writer.WriteInt(EndTick);
        writer.WriteInt(Metadata.Id);
        writer.WriteShort(Metadata.Level);
        writer.WriteInt(Stacks);
        writer.WriteBool(Enabled);
    }

    // Unknown, AdditionalEffect2
    public void WriteAdditionalEffect2(IByteWriter writer) {
        writer.WriteLong();
    }
}
