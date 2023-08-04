using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record ExpBaseTable(IReadOnlyDictionary<int, IReadOnlyDictionary<int, long>> Entries) : Table;
