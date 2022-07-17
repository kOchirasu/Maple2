using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Tools;
using Maple2.Tools.Extensions;
using Serilog;

namespace Maple2.Server.Game.Model;

public class Buff : IByteSerializable {
    private readonly FieldManager field;
    private readonly AdditionalEffectMetadata metadata;
    public readonly int ObjectId;

    public readonly IActor Caster;
    public readonly IActor Owner;

    public int Id => metadata.Id;
    public short Level => metadata.Level;

    public int StartTick { get; private set; }
    public int EndTick { get; private set; }
    public int NextTick { get; private set; }
    public int ProcCount { get; private set; }
    public int Stacks { get; private set; }

    public bool Enabled => Environment.TickCount <= EndTick;

    public Buff(FieldManager field, AdditionalEffectMetadata metadata, int objectId, IActor caster, IActor owner) {
        this.field = field;
        this.metadata = metadata;
        ObjectId = objectId;

        Caster = caster;
        Owner = owner;

        // Initialize
        Stack();
        NextTick = StartTick + this.metadata.Property.DelayTick + this.metadata.Property.IntervalTick;
    }

    public void Stack() {
        Stacks = Math.Min(Stacks + 1, metadata.Property.MaxCount);
        StartTick = Environment.TickCount;
        EndTick = StartTick + metadata.Property.DurationTick;
    }

    public void Remove() {
        if (Owner.Buffs.Remove(Id, out _)) {
            field.Broadcast(BuffPacket.Remove(this));
        }
    }

    public void Sync() {
        if (!Enabled) {
            Remove();
            return;
        }

        if (!Enabled || Environment.TickCount < NextTick) {
            return;
        }

        ProcCount++;

        ApplyRecovery();
        ApplyDotDamage();
        ApplyDotBuff();

        foreach (SkillEffectMetadata effect in metadata.Skills) {
            if (effect.Condition != null) {
                Log.Information("Buff Condition-Effect unimplemented");
            } else if (effect.Splash != null) {
                field.AddSkill(Caster, effect, new[]{Owner.Position.Align()}, Owner.Position, Owner.Rotation);
            }
        }

        // Buffs with IntervalTick=0 will just proc a single time
        if (metadata.Property.IntervalTick == 0) {
            NextTick = EndTick + 1;
        } else {
            NextTick += metadata.Property.IntervalTick;
        }
    }

    private void ApplyRecovery() {
        if (metadata.Recovery == null) {
            return;
        }

        var record = new HealDamageRecord(Caster, Owner, ObjectId, metadata.Recovery);
        var updated = new List<StatAttribute>(3);
        if (record.HpAmount != 0) {
            Owner.Stats[StatAttribute.Health].Add(record.HpAmount);
            updated.Add(StatAttribute.Health);
        }
        if (record.SpAmount != 0) {
            Owner.Stats[StatAttribute.Spirit].Add(record.SpAmount);
            updated.Add(StatAttribute.Spirit);
        }
        if (record.EpAmount != 0) {
            Owner.Stats[StatAttribute.Stamina].Add(record.EpAmount);
            updated.Add(StatAttribute.Stamina);
        }

        if (updated.Count > 0) {
            field.Broadcast(StatsPacket.Update(Owner, updated.ToArray()));
        }
        field.Broadcast(SkillDamagePacket.Heal(record));
    }

    private void ApplyDotDamage() {
        if (metadata.Dot.Damage == null) {
            return;
        }

        var record = new DotDamageRecord(Caster, Owner, metadata.Dot.Damage) {
            ProcCount = ProcCount,
        };
        var targetUpdated = new List<StatAttribute>(3);
        if (record.HpAmount != 0) {
            Owner.Stats[StatAttribute.Health].Add(record.HpAmount);
            targetUpdated.Add(StatAttribute.Health);
        }
        if (record.SpAmount != 0) {
            Owner.Stats[StatAttribute.Spirit].Add(record.SpAmount);
            targetUpdated.Add(StatAttribute.Spirit);
        }
        if (record.EpAmount != 0) {
            Owner.Stats[StatAttribute.Stamina].Add(record.EpAmount);
            targetUpdated.Add(StatAttribute.Stamina);
        }

        if (targetUpdated.Count <= 0) {
            return;
        }

        field.Broadcast(StatsPacket.Update(Owner, targetUpdated.ToArray()));
        field.Broadcast(SkillDamagePacket.DotDamage(record));
        if (record.RecoverHp != 0) {
            Caster.Stats[StatAttribute.Health].Add(record.RecoverHp);
            field.Broadcast(StatsPacket.Update(Caster, StatAttribute.Health));
        }
    }

    private void ApplyDotBuff() {
        if (metadata.Dot.Buff == null) {
            return;
        }

        AdditionalEffectMetadataDot.DotBuff dotBuff = metadata.Dot.Buff;
        if (field.SkillMetadata.TryGetEffect(dotBuff.Id, dotBuff.Level, out AdditionalEffectMetadata? additionalEffect)) {
            foreach (SkillEffectMetadata skill in additionalEffect.Skills) {
                Caster.ApplyEffect(Caster, skill);
            }
        } else {
            Log.Error("Invalid DotBuff: {SkillId},{Level}", dotBuff.Id, dotBuff.Level);
        }
    }

    public void WriteTo(IByteWriter writer) {
        WriteAdditionalEffect(writer);
        WriteAdditionalEffect2(writer);
    }

    // AdditionalEffect
    public void WriteAdditionalEffect(IByteWriter writer) {
        writer.WriteInt(StartTick);
        writer.WriteInt(EndTick);
        writer.WriteInt(Id);
        writer.WriteShort(Level);
        writer.WriteInt(Stacks);
        writer.WriteBool(Enabled);
    }

    // Unknown, AdditionalEffect2
    public void WriteAdditionalEffect2(IByteWriter writer) {
        writer.WriteLong();
    }
}
