using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Core.PacketHandlers;

public abstract class ResponseVersionHandler<T> : PacketHandler<T> where T : Session {
    public override ushort OpCode => RecvOp.RESPONSE_VERSION;

    protected ResponseVersionHandler(ILogger logger) : base(logger) { }

    public override void Handle(T session, IByteReader packet) {
        uint version = packet.Read<uint>();
        // +4 Bytes Short(2F 00) Short(02 00)

        if (version != Session.VERSION) {
            session.Disconnect();
        }
    }
}
