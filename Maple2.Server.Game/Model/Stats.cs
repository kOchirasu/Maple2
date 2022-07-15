using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Formulas;

namespace Maple2.Server.Game.Model;

public class Stats {
    public const int TOTAL = 35;

    private readonly Dictionary<StatAttribute, Stat> values;

    public int GearScore => 12345;

    public Stats(JobCode jobCode, short level) {
        values = new Dictionary<StatAttribute, Stat>();
        Reset(jobCode, level);
    }

    public Stats(NpcMetadataStat npcStats) {
        values = new Dictionary<StatAttribute, Stat>();
        foreach ((StatAttribute attribute, long value) in npcStats.Stats) {
            this[attribute].AddBase(value);
        }
    }

    public void Reset(JobCode jobCode, short level) {
        values.Clear();

        this[StatAttribute.Strength].AddBase(BaseStat.Strength(jobCode, level));
        this[StatAttribute.Dexterity].AddBase(BaseStat.Dexterity(jobCode, level));
        this[StatAttribute.Intelligence].AddBase(BaseStat.Intelligence(jobCode, level));
        this[StatAttribute.Luck].AddBase(BaseStat.Luck(jobCode, level));
        this[StatAttribute.Health].AddBase(BaseStat.Health(jobCode, level));
        this[StatAttribute.HpRegen] = new Stat(10);
        this[StatAttribute.HpRegenInterval] = new Stat(3000);
        this[StatAttribute.Spirit] = new Stat(100);
        this[StatAttribute.SpRegen] = new Stat(10);
        this[StatAttribute.SpRegenInterval] = new Stat(1000);
        this[StatAttribute.Stamina] = new Stat(120);
        this[StatAttribute.StaminaRegen] = new Stat(10);
        this[StatAttribute.StaminaRegenInterval] = new Stat(500);
        this[StatAttribute.AttackSpeed] = new Stat(100);
        this[StatAttribute.MovementSpeed] = new Stat(100);
        this[StatAttribute.Accuracy].AddBase(BaseStat.Accuracy(jobCode, level));
        this[StatAttribute.Evasion].AddBase(BaseStat.Evasion(jobCode, level));
        this[StatAttribute.CriticalRate].AddBase(BaseStat.CriticalRate(jobCode, level));
        this[StatAttribute.CriticalDamage].AddBase(BaseStat.CriticalDamage(jobCode, level));
        this[StatAttribute.CriticalEvasion].AddBase(BaseStat.CriticalEvasion(jobCode, level));
        this[StatAttribute.Defense].AddBase(BaseStat.Defense(jobCode, level));
        this[StatAttribute.JumpHeight] = new Stat(100);
        this[StatAttribute.PhysicalRes].AddBase(BaseStat.PhysicalRes(jobCode, level));
        this[StatAttribute.MagicalRes].AddBase(BaseStat.MagicalRes(jobCode, level));
        this[StatAttribute.MountSpeed] = new Stat(100);

#if DEBUG
        this[StatAttribute.AttackSpeed].AddTotal(40);
        this[StatAttribute.MovementSpeed].AddTotal(40);
        this[StatAttribute.JumpHeight].AddTotal(40);
        this[StatAttribute.MountSpeed].AddTotal(60);
#endif
    }

    public Stat this[StatAttribute attribute] {
        get {
            if (!values.ContainsKey(attribute)) {
                values[attribute] = new Stat(0, 0, 0);
            }

            return values[attribute];
        }
        set => values[attribute] = value;
    }
}

public sealed class Stat {
    public const int TOTAL = 3;

    public long Total { get; set; }
    public long Base { get; set; }
    public long Current { get; set; }

    public Stat(long value) : this(value, value, value) { }

    public Stat(long total, long @base, long current) {
        Total = total;
        Base = @base;
        Current = current;
    }

    public void AddBase(long amount) {
        Total += amount;
        Base += amount;
        Current += amount;
    }

    public void AddTotal(long amount) {
        Total += amount;
        Current += amount;
    }

    public void Add(long amount) {
        Current = Math.Clamp(Current + amount, 0, Total);
    }

    public long this[int i] {
        get {
            return i switch {
                0 => Total,
                1 => Base,
                _ => Current,
            };
        }
    }

    public override string ToString() => $"<{Total}|{Base}|{Current}>";
}
