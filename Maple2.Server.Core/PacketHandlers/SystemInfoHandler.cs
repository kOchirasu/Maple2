using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Core.PacketHandlers;

public abstract class SystemInfoHandler<T> : IPacketHandler<T> where T : Session {
    public ushort OpCode => RecvOp.SYSTEM_INFO;

    private readonly ILogger logger;

    public SystemInfoHandler(ILogger logger) {
        this.logger = logger;
    }

    public void Handle(T session, IByteReader packet) {
        string info = packet.ReadUnicodeString();
        logger.LogDebug("System Info: {Info}", info);
    }

    public abstract override string ToString();
}
