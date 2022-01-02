using Maple2.PacketLib.Tools;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public abstract class GamePacketHandler : IPacketHandler<GameSession> {
    public abstract ushort OpCode { get; }

    protected readonly ILogger logger;

    protected GamePacketHandler(ILogger logger) {
        this.logger = logger;
    }

    public abstract void Handle(GameSession session, IByteReader packet);

    public override string ToString() => $"[0x{OpCode:X4}] Game.{GetType().Name}";
}
