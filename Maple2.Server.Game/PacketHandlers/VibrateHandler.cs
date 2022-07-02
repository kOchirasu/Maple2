using System.Numerics;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class VibrateHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Vibrate;

    public override void Handle(GameSession session, IByteReader packet) {
        string entityId = packet.ReadString();
        var attack = new SkillAttack {
            Id = packet.ReadLong(),
            SkillId = packet.ReadInt(),
            SkillLevel = packet.ReadShort(),
            MotionPoint = packet.ReadByte(),
            AttackPoint = packet.ReadByte(),
        };
        packet.ReadInt(); // ServerTick
        var position = packet.Read<Vector3>();

        session.Field?.Multicast(VibratePacket.Attack(entityId, attack, position));
    }
}
