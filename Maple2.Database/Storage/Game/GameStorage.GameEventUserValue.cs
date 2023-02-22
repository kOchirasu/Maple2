using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using GameEventUserValue = Maple2.Model.Game.GameEventUserValue;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public IDictionary<GameEventUserValueType, GameEventUserValue> GetEventUserValues(long characterId) {
            var results = new Dictionary<GameEventUserValueType, GameEventUserValue>();
            foreach (Model.Event.GameEventUserValue gameEventUserValue in Context.GameEventUserValue.Where(model => model.CharacterId == characterId)) {
                results.Add(gameEventUserValue.Type, gameEventUserValue);
            }
            return results;
        }

        public bool SaveGameEventUserValues(long characterId, IEnumerable<GameEventUserValue> values) {
            foreach (GameEventUserValue value in values) {
                Model.Event.GameEventUserValue model = value;
                model.CharacterId = characterId;
                Context.GameEventUserValue.Update(model);
            }

            return Context.TrySaveChanges();
        }
    }
}
