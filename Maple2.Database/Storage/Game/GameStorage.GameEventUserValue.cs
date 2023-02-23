using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Microsoft.EntityFrameworkCore;
using GameEventUserValue = Maple2.Model.Game.GameEventUserValue;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IDictionary<GameEventUserValueType, GameEventUserValue> GetEventUserValues(long characterId) {
            return Context.GameEventUserValue.Where(model => model.CharacterId == characterId)
                .ToDictionary(model => model.Type, model => (GameEventUserValue) model);
        }

        public bool SaveGameEventUserValues(long characterId, IList<GameEventUserValue> values) {
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

            var existing = Context.GameEventUserValue.Where(model => model.CharacterId == characterId)
                .ToDictionary(model => model.Type, model => model);

            foreach (GameEventUserValue value in values) {
                if (existing.TryGetValue(value.Type, out Model.Event.GameEventUserValue? model)) {
                    model.Value = value.Value;
                    Context.GameEventUserValue.Update(model);
                } else {
                    model = value;
                    model.CharacterId = characterId;
                    Context.GameEventUserValue.Add(model);
                }
            }

            return Context.TrySaveChanges();
        }
    }
}
