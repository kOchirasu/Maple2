using System.Collections.Generic;
using System.Runtime.InteropServices;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class GameEventUserValue : IByteSerializable {
    public GameEventUserValueType Type;
    public string Value;
    public int EventId;
    public long ExpirationTime;

    public GameEventUserValue() {
    }

    public void WriteTo(IByteWriter writer) {
        writer.Write<GameEventUserValueType>(Type);
        writer.WriteInt(EventId);
        writer.WriteUnicodeString(Value);
        writer.WriteLong(ExpirationTime);
    }
}
