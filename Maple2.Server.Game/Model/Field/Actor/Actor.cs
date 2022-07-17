using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Tools.Extensions;
using Maple2.Tools.Scheduler;
using Serilog;

namespace Maple2.Server.Game.Model;

public abstract class ActorBase<T> : IActor<T> {
    private int idCounter;

    /// <summary>
    /// Generates an ObjectId unique to this specific actor instance.
    /// </summary>
    /// <returns>Returns a local ObjectId</returns>
    protected int NextLocalId() => Interlocked.Increment(ref idCounter);

    protected readonly ILogger logger = Log.ForContext<T>();

    public FieldManager Field { get; }
    public T Value { get; }

    public virtual IReadOnlyDictionary<int, Buff> Buffs => IActor.NoBuffs;
    public virtual Stats Stats { get; } = new(0, 0);

    public int ObjectId { get; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public virtual bool IsDead { get; protected set; }
    public virtual ActorState State { get; set; }
    public virtual ActorSubState SubState { get; set; }

    protected ActorBase(FieldManager field, int objectId, T value) {
        Field = field;
        ObjectId = objectId;
        Value = value;
    }

    public virtual void ApplyEffect(IActor owner, SkillEffectMetadata effect) { }

    public virtual void Sync() { }
}

/// <summary>
/// Actor is an ActorBase that can engage in combat.
/// </summary>
/// <typeparam name="T">The type contained by this object</typeparam>
public abstract class Actor<T> : ActorBase<T>, IDisposable {
    protected readonly EventQueue Scheduler;

    protected bool BroadcastBuffs { get; init; }
    protected readonly ConcurrentDictionary<int, Buff> buffs = new();
    public override IReadOnlyDictionary<int, Buff> Buffs => buffs;

    protected Actor(FieldManager field, int objectId, T value) : base(field, objectId, value) {
        Scheduler = new EventQueue();
    }

    public override void ApplyEffect(IActor owner, SkillEffectMetadata effect) {
        Debug.Assert(effect.Condition != null);

        foreach (SkillEffectMetadata.Skill skill in effect.Skills) {
            if (buffs.TryGetValue(skill.Id, out Buff? existing)) {
                existing.Stack();
                if (BroadcastBuffs) {
                    Field.Broadcast(BuffPacket.Update(existing));
                }
                continue;
            }

            if (!Field.SkillMetadata.TryGetEffect(skill.Id, skill.Level, out AdditionalEffectMetadata? additionalEffect)) {
                return;
            }

            var buff = new Buff(NextLocalId(), additionalEffect, owner, this);
            buffs[skill.Id] = buff;
            if (BroadcastBuffs) {
                Field.Broadcast(BuffPacket.Add(buff));
            }
        }
    }

    public override void Sync() {
        Scheduler.InvokeAll();

        if (IsDead) {
            return;
        }

        if (Stats[StatAttribute.Health].Current <= 0) {
            IsDead = true;
            OnDeath();
            return;
        }

        foreach ((int id, Buff buff) in buffs) {
            if (!buff.Enabled) {
                if (buffs.Remove(id, out _)) {
                    if (BroadcastBuffs) {
                        Field.Broadcast(BuffPacket.Remove(buff));
                    }
                }
            }

            if (!buff.ShouldProc()) {
                continue;
            }

            if (buff.Metadata.Recovery != null) {
                var record = new HealDamageRecord(buff.Caster, buff.Target, buff.ObjectId, buff.Metadata.Recovery);
                var updated = new List<StatAttribute>(3);
                if (record.HpAmount != 0) {
                    Stats[StatAttribute.Health].Add(record.HpAmount);
                    updated.Add(StatAttribute.Health);
                }
                if (record.SpAmount != 0) {
                    Stats[StatAttribute.Spirit].Add(record.SpAmount);
                    updated.Add(StatAttribute.Spirit);
                }
                if (record.EpAmount != 0) {
                    Stats[StatAttribute.Stamina].Add(record.EpAmount);
                    updated.Add(StatAttribute.Stamina);
                }

                if (updated.Count > 0) {
                    Field.Broadcast(StatsPacket.Update(this, updated.ToArray()));
                }
                Field.Broadcast(SkillDamagePacket.Heal(record));
            }

            if (buff.Metadata.Dot.Damage != null) {
                logger.Information("Actor DotDamage unimplemented");
            }

            if (buff.Metadata.Dot.Buff != null) {
                logger.Information("Actor DotBuff unimplemented");
            }

            foreach (SkillEffectMetadata effect in buff.Metadata.Skills) {
                if (effect.Condition != null) {
                    logger.Information("Actor Skill Condition-Effect unimplemented");
                } else if (effect.Splash != null) {
                    Field.AddSkill(buff.Caster, effect, new[]{Position.Align()}, Position, Rotation);
                }
            }
        }
    }

    public void Dispose() {
        Scheduler.Stop();
    }

    protected abstract void OnDeath();
}
