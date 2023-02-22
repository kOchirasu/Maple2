using Maple2.Model.Enum;
using Maple2.Model.Game.Event;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class GameEventUserValue : IByteSerializable {
    public long Id;
    public GameEventUserValueType Type;
    public string Value;
    public int EventId;
    public long ExpirationTime;

    public GameEventUserValue() {
        Value = string.Empty;
    }

    public GameEventUserValue(GameEventUserValueType type, GameEvent gameEvent) {
        Value = string.Empty;
        Type = type;
        ExpirationTime = gameEvent.EndTime;
        EventId = gameEvent.Id;
    }

    public int Int() => int.TryParse(Value, out int result) ? result : 0;

    public long Long() => long.TryParse(Value, out long result) ? result : 0;

    public string String() => Value;

    public bool Bool() => bool.TryParse(Value, out bool result) && result;

    public void WriteTo(IByteWriter writer) {
        writer.Write<GameEventUserValueType>(Type);
        writer.WriteInt(EventId);
        writer.WriteUnicodeString(Value);
        writer.WriteLong(ExpirationTime);
    }
}
