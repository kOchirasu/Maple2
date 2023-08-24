using System.Collections.Generic;
using Maple2.Model.Common;

namespace Maple2.Model.Metadata;

public record MeretMarketCategoryTable(IReadOnlyDictionary<int, IReadOnlyDictionary<int, MeretMarketCategoryTable.Tab>> Entries) : Table {

    public record Tab(List<string> Categories,
                      bool SortGender,
                      bool SortJob,
                      List<int> SubTabIds);
}
