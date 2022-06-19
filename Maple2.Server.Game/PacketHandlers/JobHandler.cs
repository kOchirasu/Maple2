using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class JobHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.JOB;

    private enum Command : byte {
        Unknown = 7,
        Load = 8,
        Update = 9,
        Reset = 10,
        AutoDistribute = 11,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Unknown:
                packet.ReadInt();
                packet.ReadLong();
                packet.ReadInt();
                packet.ReadShort();
                return;
            case Command.Load:
                HandleLoad(session);
                return;
            case Command.Update:
                HandleUpdate(session, packet);
                return;
            case Command.Reset:
                HandleReset(session, packet);
                return;
            case Command.AutoDistribute:
                AutoDistribute(session, packet);
                return;
        }
    }

    private void HandleLoad(GameSession session) {
        session.Send(JobPacket.Load(session.Player, session.Config.Skill.SkillInfo));
    }

    private void HandleUpdate(GameSession session, IByteReader packet) {
        int count = packet.ReadInt();
        for (int i = 0; i < count; i++) {
            int skillId = packet.ReadInt();
            short points = packet.ReadShort();
            bool enabled = packet.ReadBool();

            session.Config.Skill.UpdateSkill(skillId, points, enabled);
        }

        session.Send(JobPacket.Update(session.Player, session.Config.Skill.SkillInfo));
    }

    private void HandleReset(GameSession session, IByteReader packet) {
        var rank = (SkillRank) packet.ReadInt();
        session.Config.Skill.ResetSkills(rank);

        session.Send(JobPacket.Reset(session.Player, session.Config.Skill.SkillInfo));
    }

    private void AutoDistribute(GameSession session, IByteReader packet) {
        int count = packet.ReadInt();
        for (int i = 0; i < count; i++) {
            int skillId = packet.ReadInt();
            short points = packet.ReadShort();
            bool enabled = packet.ReadBool();

            session.Config.Skill.UpdateSkill(skillId, points, enabled);
        }

        session.Send(JobPacket.AutoDistribute(session.Player, session.Config.Skill.SkillInfo));
    }
}
