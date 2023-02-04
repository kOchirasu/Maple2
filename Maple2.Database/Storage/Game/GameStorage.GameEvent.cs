using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Game.Event;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IList<GameEvent> GetEvents() {
            return Context.GameEvent.Where(model => model.BeginTime <= DateTimeOffset.UtcNow && model.EndTime >= DateTimeOffset.UtcNow).Select(@event => new GameEvent {
                    Name = @event.Name,
                    Id = @event.Id,
                    EventInfo = @event.EventInfo,
                })
                .ToList();
        }

        public GameEvent? FindEvent(string name) {
            return Context.GameEvent.FirstOrDefault(@event => @event.BeginTime <= DateTimeOffset.UtcNow && @event.EndTime >= DateTimeOffset.UtcNow && @event.Name == name);
        }
    }
}
