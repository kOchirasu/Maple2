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

    public FieldManager Field { get; }
    public T Value { get; }

    public virtual IReadOnlyDictionary<int, Buff> Buffs => IActor.NoBuffs;
    public virtual Stats Stats { get; } = new(0, 0);

    public int ObjectId { get; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public virtual ActorState State { get; set; }
    public virtual ActorSubState SubState { get; set; }

    protected ActorBase(FieldManager field, int objectId, T value) {
        Field = field;
        ObjectId = objectId;
        Value = value;
    }

    public virtual void ApplyAttack(SkillAttack attack) { }

    public virtual void ApplyEffect(IActor owner, SkillEffectMetadata effect) { }

    public virtual void Sync() { }
}

/// <summary>
/// Actor is an ActorBase that can engage in combat.
/// </summary>
/// <typeparam name="T">The type contained by this object</typeparam>
public abstract class Actor<T> : ActorBase<T>, IDisposable {
    protected readonly EventQueue Scheduler;

    protected readonly ConcurrentDictionary<int, Buff> buffs = new();
    public override IReadOnlyDictionary<int, Buff> Buffs => buffs;

    protected Actor(FieldManager field, int objectId, T value) : base(field, objectId, value) {
        Scheduler = new EventQueue();
    }

    public override void ApplyAttack(SkillAttack attack) {
        if (attack.Damage.Count > 0) {
            Log.Debug("Actor Damage unimplemented");
        }

        foreach (SkillEffectMetadata effect in attack.Effects) {
            if (effect.Condition != null) {
                // ConditionSkill
                switch (effect.Condition.Target) {
                    case SkillEntity.Owner:
                        break;
                    case SkillEntity.Target:
                        break;
                    case SkillEntity.Caster:
                        break;
                    case SkillEntity.PetOwner:
                        break;
                    case SkillEntity.Attacker:
                        break;
                    case SkillEntity.RegionBuff:
                    case SkillEntity.RegionDebuff:
                        break;
                    case SkillEntity.RegionPet:
                        break;
                }
            } else if (effect.Splash != null) {
                Log.Debug("Actor Splash Skill unimplemented");
            }
        }
    }

    public override void ApplyEffect(IActor owner, SkillEffectMetadata effect) {
        Debug.Assert(effect.Condition != null);

        foreach (SkillEffectMetadata.Skill skill in effect.Skills) {
            if (buffs.TryGetValue(skill.Id, out Buff? existing)) {
                existing.Stack();
                Field.Broadcast(BuffPacket.Update(existing));
                continue;
            }

            if (!Field.SkillMetadata.TryGetEffect(skill.Id, skill.Level, out AdditionalEffectMetadata? additionalEffect)) {
                return;
            }

            var buff = new Buff(NextLocalId(), additionalEffect, owner, this);
            buffs[skill.Id] = buff;
            Field.Broadcast(BuffPacket.Add(buff));
        }
    }

    public override void Sync() {
        Scheduler.InvokeAll();
    }

    public void Dispose() {
        Scheduler.Stop();
    }
}
