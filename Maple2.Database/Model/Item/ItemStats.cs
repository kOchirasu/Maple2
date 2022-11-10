using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Maple2.Model.Enum;
using Maple2.Model.Game;

namespace Maple2.Database.Model;

internal record ItemStats(Dictionary<StatAttribute, StatOption>[] StatOption,
        Dictionary<SpecialAttribute, SpecialOption>[] SpecialOption) {
    [return:NotNullIfNotNull("other")]
    public static implicit operator ItemStats?(Maple2.Model.Game.ItemStats? other) {
        if (other == null) {
            return null;
        }

        Maple2.Model.Game.ItemStats.Type[] values =Enum.GetValues<Maple2.Model.Game.ItemStats.Type>();
        var statOption = new Dictionary<StatAttribute, StatOption>[values.Length];
        var specialOption = new Dictionary<SpecialAttribute, SpecialOption>[values.Length];
        foreach (Maple2.Model.Game.ItemStats.Type type in values) {
            statOption[(int) type] = other.GetStatOptions(type);
            specialOption[(int) type] = other.GetSpecialOptions(type);
        }

        return new ItemStats(statOption, specialOption);
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.ItemStats?(ItemStats? other) {
        return other == null ? null : new Maple2.Model.Game.ItemStats(other.StatOption, other.SpecialOption);
    }
}

internal record ItemEnchant(int Enchants, int EnchantExp, byte EnchantCharges, bool CanRepack, int Charges,
        Dictionary<StatAttribute, StatOption> StatOptions) {
    [return:NotNullIfNotNull("other")]
    public static implicit operator ItemEnchant?(Maple2.Model.Game.ItemEnchant? other) {
        return other == null ? null : new ItemEnchant(other.Enchants, other.EnchantExp, other.EnchantCharges,
            other.CanRepack, other.Charges, other.StatOptions);
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.ItemEnchant?(ItemEnchant? other) {
        return other == null ? null : new Maple2.Model.Game.ItemEnchant(other.Enchants, other.EnchantExp,
            other.EnchantCharges, other.CanRepack, other.Charges, other.StatOptions);
    }
}

internal record ItemLimitBreak(int Level, IDictionary<StatAttribute, StatOption> StatOptions,
        IDictionary<SpecialAttribute, SpecialOption> SpecialOptions) {
    [return:NotNullIfNotNull("other")]
    public static implicit operator ItemLimitBreak?(Maple2.Model.Game.ItemLimitBreak? other) {
        return other == null ? null : new ItemLimitBreak(other.Level, other.StatOptions, other.SpecialOptions);
    }

    [return:NotNullIfNotNull("other")]
    public static implicit operator Maple2.Model.Game.ItemLimitBreak?(ItemLimitBreak? other) {
        return other == null ? null :
            new Maple2.Model.Game.ItemLimitBreak(other.Level, other.StatOptions, other.SpecialOptions);
    }
}
