using Maple2.PacketLib.Tools;
using Maple2.Server.Constants;
using Maple2.Server.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.PacketHandlers.Common;

public class SystemInfoHandler : CommonPacketHandler {
    public override ushort OpCode => RecvOp.SYSTEM_INFO;

    public SystemInfoHandler(ILogger<SystemInfoHandler> logger) : base(logger) { }

    protected override void HandleCommon(Session session, IByteReader packet) {
        string info = packet.ReadUnicodeString();
        logger.LogDebug("System Info: {Info}", info);
    }
}
