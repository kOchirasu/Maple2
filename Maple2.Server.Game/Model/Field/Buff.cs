using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Tools;
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

    public bool Enabled => EndTick == StartTick || NextTick <= EndTick || Environment.TickCount <= EndTick;

    private readonly ILogger logger = Log.ForContext<Buff>();

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
        int tickNow = Environment.TickCount;
        if (EndTick > StartTick && tickNow > EndTick && NextTick > EndTick) {
            Remove();
            return;
        }
        // Buffs with KeepCondition == 99 will not proc.
        if (metadata.Property.KeepCondition == 99) {
            return;
        }
        if (tickNow < NextTick) {
            return;
        }

        ProcCount++;

        ApplyRecovery();
        ApplyDotDamage();
        ApplyDotBuff();

        foreach (SkillEffectMetadata effect in metadata.Skills) {
            if (effect.Condition != null) {
                IActor skillOwner = effect.Condition.Owner switch {
                    SkillEntity.Caster => Caster,
                    _ => Owner,
                };
                // logger.Error("Buff Condition-Effect unimplemented from {Id} on {Owner}", Id, Owner.ObjectId);
                switch (effect.Condition.Target) {
                    case SkillEntity.Target:
                        Owner.ApplyEffect(skillOwner, effect);
                        break;
                    case SkillEntity.Caster:
                        Caster.ApplyEffect(skillOwner, effect);
                        break;
                    default:
                        logger.Error("Invalid Buff Target: {Target}", effect.Condition.Target);
                        break;
                }
            } else if (effect.Splash != null) {
                field.AddSkill(Caster, effect, new[]{Owner.Position}, Owner.Position, Owner.Rotation);
            }
        }

        // Buffs with IntervalTick=0 will just proc a single time
        if (metadata.Property.IntervalTick == 0) {
            NextTick = int.MaxValue;
        } else {
            NextTick += metadata.Property.IntervalTick;
        }
    }

    private void ApplyRecovery() {
        if (metadata.Recovery == null) {
            return;
        }

        var record = new HealDamageRecord(Caster, Owner, ObjectId, metadata.Recovery);
        var updated = new List<BasicAttribute>(3);
        if (record.HpAmount != 0) {
            Owner.Stats[BasicAttribute.Health].Add(record.HpAmount);
            updated.Add(BasicAttribute.Health);
        }
        if (record.SpAmount != 0) {
            Owner.Stats[BasicAttribute.Spirit].Add(record.SpAmount);
            updated.Add(BasicAttribute.Spirit);
        }
        if (record.EpAmount != 0) {
            Owner.Stats[BasicAttribute.Stamina].Add(record.EpAmount);
            updated.Add(BasicAttribute.Stamina);
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
        var targetUpdated = new List<BasicAttribute>(3);
        if (record.HpAmount != 0) {
            Owner.Stats[BasicAttribute.Health].Add(record.HpAmount);
            targetUpdated.Add(BasicAttribute.Health);
        }
        if (record.SpAmount != 0) {
            Owner.Stats[BasicAttribute.Spirit].Add(record.SpAmount);
            targetUpdated.Add(BasicAttribute.Spirit);
        }
        if (record.EpAmount != 0) {
            Owner.Stats[BasicAttribute.Stamina].Add(record.EpAmount);
            targetUpdated.Add(BasicAttribute.Stamina);
        }

        if (targetUpdated.Count <= 0) {
            return;
        }

        field.Broadcast(StatsPacket.Update(Owner, targetUpdated.ToArray()));
        field.Broadcast(SkillDamagePacket.DotDamage(record));
        if (record.RecoverHp != 0) {
            Caster.Stats[BasicAttribute.Health].Add(record.RecoverHp);
            field.Broadcast(StatsPacket.Update(Caster, BasicAttribute.Health));
        }
    }

    private void ApplyDotBuff() {
        if (metadata.Dot.Buff == null) {
            return;
        }

        AdditionalEffectMetadataDot.DotBuff dotBuff = metadata.Dot.Buff;
        if (dotBuff.Target == SkillEntity.Target) {
            Owner.AddBuff(Caster, dotBuff.Id, dotBuff.Level);
        } else {
            Caster.AddBuff(Caster, dotBuff.Id, dotBuff.Level);
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
