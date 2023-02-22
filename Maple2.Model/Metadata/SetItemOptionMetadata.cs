using Maple2.Model.Enum;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;

namespace Maple2.Model.Metadata;

public record SetItemInfoMetadata(
    int Id,
    int[] ItemIds,
    int OptionId);

public record SetItemOptionMetadata(
    int Id,
    SetBonusMetadata[] Parts);

public record SetBonusMetadata(
    int Count,
    int[] AdditionalEffectIds,
    short[] AdditionalEffectLevels,
    IReadOnlyDictionary<BasicAttribute, long> Values,
    IReadOnlyDictionary<BasicAttribute, float> Rates,
    IReadOnlyDictionary<SpecialAttribute, float> SpecialValues,
    IReadOnlyDictionary<SpecialAttribute, float> SpecialRates,
    int SgiTarget,
    int SgiBossTarget);