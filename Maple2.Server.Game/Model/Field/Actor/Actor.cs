using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Tools.Scheduler;
using Serilog;

namespace Maple2.Server.Game.Model;

public abstract class ActorBase<T> : IActor<T> {
    public FieldManager Field { get; }
    public T Value { get; }

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

    public virtual void ApplyEffect(IFieldEntity owner, SkillEffectMetadata effect) { }

    public virtual void Sync() { }
}

/// <summary>
/// Actor is an ActorBase that can engage in combat.
/// </summary>
/// <typeparam name="T">The type contained by this object</typeparam>
public abstract class Actor<T> : ActorBase<T>, IDisposable {
    public abstract IReadOnlyDictionary<int, Buff> Buffs { get; }
    public abstract Stats Stats { get; }

    protected readonly EventQueue Scheduler;

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
                        break;
                    case SkillEntity.RegionDebuff:
                        break;
                    case SkillEntity.RegionPet:
                        break;
                }
            } else if (effect.Splash != null) {
                // SplashSkill
            }
        }
    }

    public override void ApplyEffect(IFieldEntity owner, SkillEffectMetadata effect) {

    }

    public override void Sync() {
        Scheduler.InvokeAll();
    }

    public void Dispose() {
        Scheduler.Stop();
    }
}
