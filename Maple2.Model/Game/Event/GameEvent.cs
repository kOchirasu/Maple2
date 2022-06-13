using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game.Event;

public abstract class GameEvent : IByteSerializable {
    public readonly string Name;

    protected GameEvent(string name) {
        Name = name;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.WriteUnicodeString(Name);
    }
}
