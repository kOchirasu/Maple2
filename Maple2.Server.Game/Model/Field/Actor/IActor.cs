﻿using System.Collections.Concurrent;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager;
using Maple2.Server.Game.Manager.Config;
using Maple2.Server.Game.Model.Skill;
using Maple2.Tools.Collision;

namespace Maple2.Server.Game.Model;

public interface IActor : IFieldEntity {
    public BuffManager Buffs { get; }
    public StatsManager Stats { get; }


    public bool IsDead { get; }
    public IPrism Shape { get; }

    public virtual void ApplyEffect(IActor caster, IActor owner, SkillEffectMetadata effect) { }
    public virtual void ApplyDamage(IActor caster, DamageRecord damage, SkillMetadataAttack attack) { }
    public virtual void AddBuff(IActor caster, IActor owner, int id, short level, bool notifyField = true) { }

    public virtual void TargetAttack(SkillRecord record) { }
}

public interface IActor<out T> : IActor {
    public T Value { get; }
}
