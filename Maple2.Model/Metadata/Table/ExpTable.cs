using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record ExpTable(
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, long>> ExpBase,
    IReadOnlyDictionary<int, long> NextExp) : Table;