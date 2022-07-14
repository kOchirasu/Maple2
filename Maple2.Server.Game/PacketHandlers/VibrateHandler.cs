using System.Numerics;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class VibrateHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Vibrate;

    public override void Handle(GameSession session, IByteReader packet) {
        string entityId = packet.ReadString();
        var skill = new SkillRecord {
            Uid = packet.ReadLong(),
            SkillId = packet.ReadInt(),
            Level = packet.ReadShort(),
            MotionPoint = packet.ReadByte(),
            AttackPoint = packet.ReadByte(),
            ServerTick = packet.ReadInt(),
            Position = packet.Read<Vector3>(),
        };

        session.Field?.Broadcast(VibratePacket.Attack(entityId, skill));
    }
}
