using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Util;
using Maple2.Tools;
using Serilog;

namespace Maple2.Server.Game.Model;

public class Buff : IUpdatable, IByteSerializable {
    private readonly FieldManager field;
    private readonly AdditionalEffectMetadata metadata;
    public readonly int ObjectId;

    public readonly IActor Caster;
    public readonly IActor Owner;

    public int Id => metadata.Id;
    public short Level => metadata.Level;

    public long StartTick { get; private set; }
    public long EndTick { get; private set; }
    public long IntervalTick { get; private set; }
    public long NextProcTick { get; protected set; }
    public int ProcCount { get; private set; }
    public int Stacks { get; private set; }

    public bool Enabled { get; private set; }

    private bool activated;
    private readonly bool canExpire;
    private bool canProc;

    private readonly ILogger logger = Log.ForContext<Buff>();

    public Buff(FieldManager field, AdditionalEffectMetadata metadata, int objectId, IActor caster, IActor owner) {
        this.field = field;
        this.metadata = metadata;
        ObjectId = objectId;

        Caster = caster;
        Owner = owner;

        // Buffs with IntervalTick=0 will just proc a single time
        IntervalTick = metadata.Property.IntervalTick > 0 ? metadata.Property.IntervalTick : metadata.Property.DurationTick + 1000;

        Stack();
        NextProcTick = StartTick + this.metadata.Property.DelayTick + this.metadata.Property.IntervalTick;
        UpdateEnabled(false);
        // Buffs with KeepCondition == 99 will not proc?
        canProc = metadata.Property.KeepCondition != BuffKeepCondition.UnlimitedDuration;
        // Buffs with a duration of 0 are permanent.
        canExpire = EndTick > StartTick;
    }

    public bool Stack() {
        if (Stacks >= metadata.Property.MaxCount) {
            return false;
        }
        Stacks = Math.Min(Stacks + 1, metadata.Property.MaxCount);
        StartTick = Environment.TickCount64;
        EndTick = StartTick + metadata.Property.DurationTick;
        return true;
    }

    public void RemoveStack(int amount = 1) {
        Stacks = Math.Max(0, Stacks - amount);
        if (Stacks == 0) {
            Remove();
        }
    }

    private void Activate() {
        if (metadata.Update.Cancel != null) {
            foreach (int id in metadata.Update.Cancel.Ids) {
                if (metadata.Update.Cancel.CheckSameCaster && Owner.ObjectId != Caster.ObjectId) {
                    continue;
                }

                Owner.Buffs.Remove(id);
            }
        }

        activated = true;
    }

    private bool UpdateEnabled(bool notifyField = true) {
        bool enabled = metadata.Condition.Check(Caster, Owner, Owner);
        if (Enabled != enabled) {
            Enabled = enabled;
            if (notifyField) {
                field.Broadcast(BuffPacket.Update(this));
            }
        }

        return enabled;
    }

    public void Remove() {
        if (Owner.Buffs.Remove(Id)) {
            field.Broadcast(BuffPacket.Remove(this));
        }
    }

    public virtual void Update(long tickCount) {
        if (!activated) {
            Activate();
        }

        if (canExpire && !canProc && tickCount > EndTick) {
            Remove();
            return;
        }

        // TODO: Check conditions less frequently
        if (!UpdateEnabled()) {
            return;
        }

        if (!canProc || tickCount < NextProcTick) {
            return;
        }

        Proc();
    }

    private void Proc() {
        ProcCount++;

        ApplyRecovery();
        ApplyDotDamage();
        ApplyDotBuff();

        foreach (SkillEffectMetadata effect in metadata.Skills) {
            if (effect.Condition != null) {
                // logger.Error("Buff Condition-Effect unimplemented from {Id} on {Owner}", Id, Owner.ObjectId);
                switch (effect.Condition.Target) {
                    case SkillEntity.Target:
                        Owner.ApplyEffect(Caster, Owner, effect);
                        break;
                    case SkillEntity.Caster:
                        Caster.ApplyEffect(Caster, Owner, effect);
                        break;
                    default:
                        logger.Error("Invalid Buff Target: {Target}", effect.Condition.Target);
                        break;
                }
            } else if (effect.Splash != null) {
                field.AddSkill(Caster, effect, new[] {Owner.Position}, Owner.Rotation);
            }
        }

        NextProcTick += IntervalTick;
        if (NextProcTick > EndTick) {
            canProc = false;
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
            Owner.AddBuff(Caster, Owner, dotBuff.Id, dotBuff.Level);
        } else {
            Caster.AddBuff(Caster, Owner, dotBuff.Id, dotBuff.Level);
        }
    }

    public void WriteTo(IByteWriter writer) {
        WriteAdditionalEffect(writer);
        WriteAdditionalEffect2(writer);
    }

    // AdditionalEffect
    public void WriteAdditionalEffect(IByteWriter writer) {
        writer.WriteInt((int) StartTick);
        writer.WriteInt((int) EndTick);
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
