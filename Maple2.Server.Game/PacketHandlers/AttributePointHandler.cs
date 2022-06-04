using System.Linq;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Manager.Config;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class AttributePointHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.ATTRIBUTE_POINT;

    private enum Command : byte {
        Increment = 2,
        Reset = 3,
    }

    public AttributePointHandler(ILogger<AttributePointHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Increment:
                HandleIncrement(session, packet);
                return;
            case Command.Reset:
                HandleReset(session);
                return;
        }
    }

    private void HandleIncrement(GameSession session, IByteReader packet) {
        var type = packet.Read<StatAttribute>();
        // Limit reached or invalid type.
        if (StatAttributes.PointAllocation.StatLimit(type) <= 0) {
            return;
        }

        // No points remaining
        if (session.Config.StatAttributes.UsedPoints >= session.Config.StatAttributes.TotalPoints) {
            return;
        }

        session.Config.StatAttributes.Allocation[type]++;
        // TODO: Sync with player stats
        session.Send(AttributePointPacket.Allocation(session.Config.StatAttributes));
    }

    private void HandleReset(GameSession session) {
        foreach (StatAttribute type in session.Config.StatAttributes.Allocation.Attributes) {
            int points = session.Config.StatAttributes.Allocation[type];
            session.Config.StatAttributes.Allocation[type] = 0;
            // TODO: Sync with player stats
        }

        session.Send(AttributePointPacket.Allocation(session.Config.StatAttributes));
        session.Send(NoticePacket.Message("s_char_info_reset_stat_pointsuccess_msg"));
    }
}
