using System.Collections.Generic;

namespace Maple2.Model.Metadata;

public record NextExpTable(IReadOnlyDictionary<int, long> Entries) : Table;
