using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record ItemOptionConstantTable(
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, ItemOptionConstant>> Options
) : Table(Discriminator.ItemOptionConstantTable);

public record ItemOptionRandomTable(
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, ItemOption>> Options
) : Table(Discriminator.ItemOptionRandomTable);

public record ItemOptionStaticTable(
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, ItemOption>> Options
) : Table(Discriminator.ItemOptionStaticTable);

public record ItemOptionConstant(
    IReadOnlyDictionary<StatAttribute, int> Values,
    IReadOnlyDictionary<StatAttribute, float> Rates,
    IReadOnlyDictionary<SpecialAttribute, int> SpecialValues,
    IReadOnlyDictionary<SpecialAttribute, float> SpecialRates);

public record ItemOption(
    float MultiplyFactor,
    ItemOption.Range<int> NumPick,
    ItemOption.Entry[] Entries
) {
    public record struct Entry(
        StatAttribute? StatAttribute = null,
        SpecialAttribute? SpecialAttribute = null,
        Range<int>? Values = null,
        Range<float>? Rates = null);
    public record struct Range<T>(T Min, T Max) where T : struct;
}

public record ItemOptionPickTable(
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, ItemOptionPickTable.Option>> Options
) : Table(Discriminator.ItemOptionPickTable) {
    public record Option(
        IReadOnlyDictionary<StatAttribute, int> ConstantValue,
        IReadOnlyDictionary<StatAttribute, int> ConstantRate,
        IReadOnlyDictionary<StatAttribute, int> StaticValue,
        IReadOnlyDictionary<StatAttribute, int> StaticRate,
        IReadOnlyDictionary<StatAttribute, int> RandomValue,
        IReadOnlyDictionary<StatAttribute, int> RandomRate
    );
}

public record ItemVariationTable(
    IReadOnlyDictionary<StatAttribute, ItemVariationTable.Range<int>> Values,
    IReadOnlyDictionary<StatAttribute, ItemVariationTable.Range<float>> Rates,
    IReadOnlyDictionary<SpecialAttribute, ItemVariationTable.Range<int>> SpecialValues,
    IReadOnlyDictionary<SpecialAttribute, ItemVariationTable.Range<float>> SpecialRates
) : Table(Discriminator.ItemVariationTable) {
    public record struct Range<T>(T Min, T Max, T Interval);
}

public record ItemEquipVariationTable(
    IReadOnlyDictionary<StatAttribute, int[]> Values,
    IReadOnlyDictionary<StatAttribute, float[]> Rates,
    IReadOnlyDictionary<SpecialAttribute, int[]> SpecialValues,
    IReadOnlyDictionary<SpecialAttribute, float[]> SpecialRates
) : Table(Discriminator.ItemEquipVariationTable);
