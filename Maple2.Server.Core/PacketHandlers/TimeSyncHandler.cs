using System;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Core.PacketHandlers;

public abstract class TimeSyncHandler<T> : PacketHandler<T> where T : Session {
    public override ushort OpCode => RecvOp.REQUEST_TIME_SYNC;

    protected TimeSyncHandler(ILogger logger) : base(logger) { }

    public override void Handle(T session, IByteReader packet) {
        int key = packet.ReadInt();

        session.Send(TimeSyncPacket.Response(DateTimeOffset.UtcNow, key));
    }
}
