using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Core.PacketHandlers;

public abstract class SystemInfoHandler<T> : PacketHandler<T> where T : Session {
    public override ushort OpCode => RecvOp.SYSTEM_INFO;

    protected SystemInfoHandler(ILogger logger) : base(logger) { }

    public override void Handle(T session, IByteReader packet) {
        string info = packet.ReadUnicodeString();
        logger.LogDebug("System Info: {Info}", info);
    }
}
