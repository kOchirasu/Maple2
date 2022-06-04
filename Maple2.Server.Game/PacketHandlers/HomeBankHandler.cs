using System;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class HomeBankHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.REQUEST_HOME_BANK;

    private enum Command : byte {
        Home = 0,
        Premium = 1,
    }

    public HomeBankHandler(ILogger<HomeBankHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Home:
                long time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (session.Player.Value.Character.StorageCooldown + Constant.HomeBankCallCooldown > time) {
                    return;
                }

                session.Player.Value.Character.StorageCooldown = time;
                session.Send(HomeBank(time));
                return;
            case Command.Premium:
                session.Send(HomeBank(DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                return;
        }
    }

    private static ByteWriter HomeBank(long time) {
        var pWriter = Packet.Of(SendOp.HOME_BANK);
        pWriter.WriteLong(time);

        return pWriter;
    }
}
