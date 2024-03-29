﻿using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game.Event;

public class GameEvent {
    public int Id { get; init; }
    public string Name { get; init; }
    public long BeginTime { get; init; }
    public long EndTime { get; init; }
    public GameEventInfo EventInfo { get; init; }
}

public abstract class GameEventInfo : IByteSerializable {
    public int Id;
    public string Name;
    protected GameEventInfo() {
        Name = this.GetType().Name;
    }

    public abstract void WriteTo(IByteWriter writer);
}
