using System;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class FallDamageHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.StateFallDamage;

    private const float BASE_FALL_DISTANCE = 750f;
    private const float BLOCK_SIZE = 150f;

    public override void Handle(GameSession session, IByteReader packet) {
        float distance = packet.ReadFloat();
        int damage = CalcFallDamage(session.Player.Stats[BasicAttribute.Health].Total, distance);

        if (damage > 0) {
            // TODO: Reduce HP
            session.Send(FallDamagePacket.FallDamage(session.Player.ObjectId, damage));
        }
    }

    private static int CalcFallDamage(long maxHp, float distance) {
        float damage = maxHp * 7 * ((distance - BASE_FALL_DISTANCE) / BLOCK_SIZE) / 100;
        return Math.Max((int) damage, 0);
    }
}
