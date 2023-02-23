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

namespace Maple2.Server.Game.Manager;

public sealed class GameEventUserValueManager {

    private const int BATCH_SIZE = 10;
    private readonly GameSession session;
    private readonly IDictionary<GameEventUserValueType, GameEventUserValue> eventValues;

    public GameEventUserValueManager(GameSession session) {
        this.session = session;

        using GameStorage.Request db = session.GameStorage.Context();
        eventValues = db.GetEventUserValues(session.CharacterId);
    }

    public object this[GameEventUserValueType type] {
        set {
            if (!eventValues.TryGetValue(type, out GameEventUserValue? gameEventUserValue)) {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid event value type.");
            }
            gameEventUserValue.Value = value.ToString() ?? throw new InvalidOperationException("Invalid new value");
            session.Send(GameEventUserValuePacket.Update(gameEventUserValue));
        }
    }

    public void Load() {
        foreach (ImmutableList<GameEventUserValue> batch in eventValues.Values.Batch(BATCH_SIZE)) {
            session.Send(GameEventUserValuePacket.Load(batch));
        }
    }

    public GameEventUserValue Get(GameEventUserValueType type, GameEvent gameEvent) {
        if (!eventValues.TryGetValue(type, out GameEventUserValue? value)) {
            value = new GameEventUserValue(type, gameEvent);
            eventValues.Add(type, value);
        }

        return eventValues[type];
    }

    public void Save(GameStorage.Request db) {
        db.SaveGameEventUserValues(session.CharacterId, eventValues.Values.ToList());
    }
}
