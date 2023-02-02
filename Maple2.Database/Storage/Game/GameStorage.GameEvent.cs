using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Game.Event;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IEnumerable<GameEvent> GetEvents() {
            var results = Context.GameEvent.Where(model => model.BeginTime <= DateTime.UtcNow && model.EndTime >= DateTime.UtcNow);
            foreach (Model.Event.GameEvent model in results) {
                GameEvent result = model;
                result.EventInfo.Id = model.Id;
                result.EventInfo.Name = model.Name;
                yield return result;
            }
        }

        public GameEvent? FindEvent(string name) {
            return Context.GameEvent.FirstOrDefault(@event => @event.BeginTime <= DateTime.UtcNow && @event.EndTime >= DateTime.UtcNow && @event.Name == name);
        }
    }
}
