using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Server.Game.Model;

public class Stats {
    public const int TOTAL = 35;

    private readonly Dictionary<StatAttribute, Stat> values;

    public int GearScore => 12345;

    public Stats() {
        values = new Dictionary<StatAttribute, Stat> {
            [StatAttribute.Strength] = new Stat(1),
            [StatAttribute.Dexterity] = new Stat(1),
            [StatAttribute.Intelligence] = new Stat(1),
            [StatAttribute.Luck] = new Stat(1),
            [StatAttribute.Health] = new Stat(50),
            [StatAttribute.HpRegen] = new Stat(10, 10, 100),
            [StatAttribute.HpRegenInterval] = new Stat(3000),
            [StatAttribute.Spirit] = new Stat(100),
            [StatAttribute.SpRegen] = new Stat(10, 10, 20),
            [StatAttribute.SpRegenInterval] = new Stat(500),
            [StatAttribute.Stamina] = new Stat(120),
            [StatAttribute.StaminaRegen] = new Stat(10),
            [StatAttribute.StaminaRegenInterval] = new Stat(500),
            [StatAttribute.AttackSpeed] = new Stat(110, 100, 140),
            [StatAttribute.MovementSpeed] = new Stat(110, 100, 140),
            [StatAttribute.CriticalRate] = new Stat(35),
            [StatAttribute.JumpHeight] = new Stat(100, 100, 140),
            [StatAttribute.MountSpeed] = new Stat(100, 100, 160),
        };
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

    public void AddTotal(long amount) {
        Total += amount;
        Current += amount;
    }

    public long this[int i] {
        get {
            return i switch {
                0 => Total,
                1 => Base,
                _ => Current
            };
        }
    }

    public override string ToString() => $"<{Total}|{Base}|{Current}>";
}
