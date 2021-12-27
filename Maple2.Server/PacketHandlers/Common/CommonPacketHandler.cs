using Maple2.PacketLib.Tools;
using Maple2.Server.Network;
using Maple2.Server.Servers.Game;
using Maple2.Server.Servers.Login;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.PacketHandlers.Common; 

public abstract class CommonPacketHandler : IPacketHandler<LoginSession>, IPacketHandler<GameSession> {
    public abstract ushort OpCode { get; }

    protected readonly ILogger logger;

    protected CommonPacketHandler(ILogger logger) {
        this.logger = logger;
    }

    public virtual void Handle(GameSession session, IByteReader packet) {
        HandleCommon(session, packet);
    }

    public virtual void Handle(LoginSession session, IByteReader packet) {
        HandleCommon(session, packet);
    }

    protected abstract void HandleCommon(Session session, IByteReader packet);

    public override string ToString() => $"[0x{OpCode:X4}] Common.{GetType().Name}";
}