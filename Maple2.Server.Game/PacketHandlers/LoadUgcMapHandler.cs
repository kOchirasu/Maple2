using System.Diagnostics;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class LoadUgcMapHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.REQUEST_LOAD_UGC_MAP;

    public override void Handle(GameSession session, IByteReader packet) {
        Debug.Assert(packet.ReadInt() == GameSession.FIELD_KEY);

        // TODO: Temporary for loading
        var loadUgcMap = Packet.Of(SendOp.LOAD_UGC_MAP);
        loadUgcMap.WriteBytes(new byte[4 + 4 + 1]);
        var loadCubes = Packet.Of(SendOp.LOAD_CUBES);
        loadCubes.WriteBytes(new byte[1 + 1 + 4]);

        session.Send(loadUgcMap);
        session.Send(loadCubes);
    }
}
