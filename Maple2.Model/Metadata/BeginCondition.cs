using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record BeginCondition(
    short Level,
    long Mesos,
    Gender Gender,
    JobCode[] JobCode,
    BeginConditionTarget? Target,
    BeginConditionTarget? Owner,
    BeginConditionTarget? Caster);

public record BeginConditionTarget(
    BeginConditionTarget.HasBuff[] Buff,
    BeginConditionTarget.HasSkill? Skill,
    BeginConditionTarget.EventCondition? Event
) {
    public record HasBuff(int Id, short Level, bool Owned, int Count, CompareType Compare);
    public record HasSkill(int Id, short Level);

    // All: 1,2,4,5,6,7,10,11,13,14,16,17,18,19,20,102,103
    // IgnoreOwner => 1,2,4,5,6,7,10,16,17,18
    // SkillIds => 4,6,7,14,20
    // BuffIds => 16,17,102
    public record EventCondition(int Type, bool IgnoreOwner, int[] SkillIds, int[] BuffIds);

    //   1:
    //   2: If you're attacked within 18 sec, grants Counterattack Chance, increasing damage by 3%.
    //   4: If you're attacked within 18 sec, grants Counterattack Chance, increasing damage by 3%.
    //   5: Casting Flame Tornado temporarily grants Flame Imp.
    //   6: The eagle's majesty inspires. Restores 1 spirit every sec. The eagle also attacks on hit.
    //   7: Blocking an attack has a 40% chance to grant Counterattack Chance, increasing all damage by 3% for 18 sec.
    //  10: At 5 stacks, activates ultimate curse.
    //  11: Greatly increases movement speed for 3 sec when investigating nearby objects.
    //  13: Can be used while Meridian Flow III is active.\n\nDeals 1668% damage and removes Meridian Flow III.
    //  14: 10700021, Empowered while Cunning is active.
    //      Grants a Deflector Shield that absorbs damage equal to 30% of your max health.
    //  16: 10200261, Triggers Death Rattle when one of the following conditions is met: (Conditions: 4, 6, 16)
    //      - You use a skill that consumes Dark Aura while it is at 10 stacks (16)
    //      - You are surrounded by 8 or more enemies (6)
    //      - Your health is reduced to 10% or below. (4)
    //  17: EventEffectId
    // [Bonus Effects]\nConsume 1 Awakened Mantra Core to instantly charge to 5.\nUse Vision Torrent to turn this skill into Vision Spirit Burn.\nSoul Exhaustion prevents the use of Spirit Burn for 10 sec.\nVision Amplification increases magic damage by 12% when Vision Torrent is active.
    //
    //
    //  18: 90051156, Increases movement speed for 10 sec after Mining, Foraging, Ranching, or Farming.
    //  19: 61100139, Defense +2% for 10 sec when owner's attack misses
    //  20: EventSkillId
    // 102: 10300271, Gains a stack when Frost is inflicted.
    // 103: 11000266, Deals another hit for 500% damage after hitting a target all 9 times.

    //   1:
    //   2:
    //   4: Player Attacked
    //   5: Skill Casted? (See: InvokeEffectProperty)
    //   6: Enemies Nearby
    //   7: Blocked Attack
    //  10: Buff Stacks
    //  11: Investigating Objects
    //  13:
    //  14:
    //  16:
    //  17:
    //  18: Life Skills (Mining, Foraging, Ranching, or Farming)
    //  19: Attack Misses
    //  20:
    // 102: Frost Inflicted (10300271)
    // 103: Target Hit All 9x (11000266)
}
