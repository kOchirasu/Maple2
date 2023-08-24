using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Game;
using Maple2.Model.Game.Event;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IList<PromoBanner> GetBanners() {
            return Context.PromoBanner
                .AsEnumerable()
                .Select<Model.PromoBanner, PromoBanner>(banner => banner)
                .ToList();
        }
    }
}
