using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Core.PacketHandlers;

public abstract class RequestTimeSyncHandler<T> : IPacketHandler<T> where T : Session {
    public ushort OpCode => RecvOp.REQUEST_TIME_SYNC;

    private readonly ILogger logger;

    protected RequestTimeSyncHandler(ILogger logger) {
        this.logger = logger;
    }

    public virtual void Handle(T session, IByteReader packet) {
        int key = packet.ReadInt();

        session.Send(TimeSyncPacket.Response(key));
    }

    public abstract override string ToString();
}
