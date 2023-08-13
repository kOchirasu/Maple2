using System;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.Server.Core.Formulas;

public static class Damage {
    public static DamageType RollCritical(double casterLuck, double casterCriticalRate, double casterCriticalRateOverride, double targetCriticalEvasion) {
        casterCriticalRate *= Constant.CriticalConstant;
        casterCriticalRate += casterLuck;
        targetCriticalEvasion = Math.Max(targetCriticalEvasion, 1) * 2;
        double criticalChance = Math.Min(casterCriticalRate / targetCriticalEvasion * Constant.CriticalPercentageConversion, Constant.MaxCriticalRate);
        return Random.Shared.NextDouble() < Math.Max(criticalChance, casterCriticalRateOverride) ? DamageType.Critical : DamageType.Normal;
    }

    public static double FinalCriticalDamage(double targetCriticalDamageResistance, double casterCriticalDamage) {
        targetCriticalDamageResistance = 1 / (1 + targetCriticalDamageResistance);
        casterCriticalDamage = 1000 + casterCriticalDamage + casterCriticalDamage;
        return targetCriticalDamageResistance * (casterCriticalDamage / 1000 - 1) + 1;
    }

    public static double CalculateResistance(double targetPiercingResistance, double casterPiercingMultiplied) {
        return (1500.0 - Math.Max(0, targetPiercingResistance - 1500 * casterPiercingMultiplied)) / 1500;
    }

    public static double HitRate(float targetAccuracyResistance, long targetEvasionTotal, long targetDexeterityTotal, long casterAccuracyTotal, float casterEvasionResistance, long casterLuckTotal) {
        double casterAccuracy = casterAccuracyTotal * (1 - targetAccuracyResistance);
        double targetEvasion = targetEvasionTotal * (1 - casterEvasionResistance);

        return (casterAccuracy - 10) / (targetEvasion + (0.8 * casterAccuracy)) * 2 + Math.Min(0.05, targetDexeterityTotal / (targetDexeterityTotal + 50) * 0.05)
               - Math.Min(0.05, casterLuckTotal / (casterLuckTotal + 50) * 0.05);
    }

    public static double LuckCoefficient(JobCode jobCode) {
        return jobCode switch {
            JobCode.Newbie => 1,
            JobCode.Knight => 3.78,
            JobCode.Berserker => 4.305,
            JobCode.Wizard => 3.40375,
            JobCode.Priest => 7.34125,
            JobCode.Archer => 6.4575,
            JobCode.HeavyGunner => 2.03875,
            JobCode.Thief => 0.60375,
            JobCode.Assassin => 0.55125,
            JobCode.RuneBlader => 3.78,
            JobCode.Striker => 2.03875,
            JobCode.SoulBinder => 3.40375,
            _ => 1,
        };
    }
}
