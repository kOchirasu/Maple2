using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.Server.Game.Model.Skill;

public class ReflectRecord {
    public readonly int SourceBuffId;

    public readonly float Rate;
    public readonly int EffectId;
    public readonly short EffectLevel;
    public readonly int MaxCount;
    public int Counter = 0;
    public readonly int PhysicalRateLimit;
    public readonly int MagicalRateLimit;
    public IReadOnlyDictionary<BasicAttribute, long> ReflectValues;
    public IReadOnlyDictionary<BasicAttribute, float> ReflectRates;
    
    public ReflectRecord(int id, AdditionalEffectMetadataReflect reflect) {
        SourceBuffId = id;
        Rate = reflect.Rate;
        EffectId = reflect.EffectId;
        EffectLevel = reflect.EffectLevel;
        MaxCount = reflect.ReflectionCount;
        PhysicalRateLimit = reflect.PhysicalReflectRateLimit;
        MagicalRateLimit = reflect.MagicReflectRateLimit;
        ReflectValues = reflect.ReflectValues;
        ReflectRates = reflect.ReflectRates;
    }
}
