using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Game.Event;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Microsoft.Scripting.Utils;

namespace Maple2.Server.Game.Manager;

public sealed class GameEventUserValueManager {

    private const int BATCH_SIZE = 10;
    private readonly GameSession session;
    private readonly Dictionary<int, Dictionary<GameEventUserValueType, GameEventUserValue>> eventValues;

    public GameEventUserValueManager(GameSession session) {
        this.session = session;

        using GameStorage.Request db = session.GameStorage.Context();
        eventValues = db.GetEventUserValues(session.CharacterId);
    }

    public void Set(GameEventUserValueType type, int gameEventId, object value) {
        if (!eventValues.TryGetValue(gameEventId, out Dictionary<GameEventUserValueType, GameEventUserValue>? eventDictionary)) {
            throw new ArgumentOutOfRangeException("gameEventId", gameEventId, "Invalid game event id.");
        }
        if (!eventDictionary.TryGetValue(type, out GameEventUserValue? gameEventUserValue)) {
            throw new ArgumentOutOfRangeException("gameEventId", gameEventId, "Invalid game event id.");
        }
        
        gameEventUserValue.Value = value.ToString() ?? throw new InvalidOperationException("Invalid new value");
        session.Send(GameEventUserValuePacket.Update(gameEventUserValue));
    }

    public void Load() {
        IList<GameEventUserValue> values = new List<GameEventUserValue>();
        foreach (Dictionary<GameEventUserValueType, GameEventUserValue> dict in eventValues.Values) {
            values.AddRange(dict.Values);
        }

        foreach (ImmutableList<GameEventUserValue> batch in values.Batch(BATCH_SIZE)) {
            session.Send(GameEventUserValuePacket.Load(batch));
        }
    }

    public GameEventUserValue Get(GameEventUserValueType type, GameEvent gameEvent) {
        if (!eventValues.TryGetValue(gameEvent.Id, out Dictionary<GameEventUserValueType, GameEventUserValue>? valueDict)) {
            eventValues.Add(gameEvent.Id, new Dictionary<GameEventUserValueType, GameEventUserValue>{
                {type, new GameEventUserValue(type, gameEvent)},
            });
        } else {
            if (!valueDict.ContainsKey(type)) {
                valueDict.Add(type, new GameEventUserValue(type, gameEvent));
            }
        }

        return eventValues[gameEvent.Id][type];
    }

    public void Save(GameStorage.Request db) {
        db.SaveGameEventUserValues(session.CharacterId,eventValues.Values.SelectMany(value => value.Values).ToList());
    }
}
