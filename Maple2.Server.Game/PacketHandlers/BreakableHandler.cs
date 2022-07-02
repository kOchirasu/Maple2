using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class BreakableHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Breakable;

    public override void Handle(GameSession session, IByteReader packet) {
        string entityId = packet.ReadString();
        var attack = new SkillAttack {
            Id = packet.ReadLong(),
            SkillId = packet.ReadInt(),
            SkillLevel = packet.ReadShort(),
            MotionPoint = packet.ReadByte(),
            AttackPoint = packet.ReadByte(),
        };

        if (session.Field?.TryGetBreakable(entityId, out FieldBreakable? breakable) == true) {
            breakable.UpdateState(BreakableState.Break);
        }
    }
}
