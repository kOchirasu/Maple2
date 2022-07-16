using System.Collections.Generic;
using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.PacketHandlers;

public class SkillHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Skill;

    private enum Command : byte {
        Use = 0,
        Attack = 1,
        Sync = 2,
        TickSync = 3,
        Cancel = 4,
    }

    private enum SubCommand : byte {
        Point = 0,
        Target = 1,
        Splash = 2,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public SkillMetadataStorage SkillMetadata { private get; init; } = null!;
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Use:
                HandleUse(session, packet);
                return;
            case Command.Attack:
                var subcommand = packet.Read<SubCommand>();
                switch (subcommand) {
                    case SubCommand.Point:
                        HandlePoint(session, packet);
                        return;
                    case SubCommand.Target:
                        HandleTarget(session, packet);
                        return;
                    case SubCommand.Splash:
                        HandleSplash(session, packet);
                        return;
                }
                return;
            case Command.Sync:
                HandleSync(session, packet);
                return;
            case Command.TickSync:
                HandleTickSync(session, packet);
                return;
            case Command.Cancel:
                HandleCancel(session, packet);
                return;
        }
    }

    private void HandleUse(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        int serverTick = packet.ReadInt();
        int skillId = packet.ReadInt();
        short level = packet.ReadShort();
        if (!SkillMetadata.TryGet(skillId, level, out SkillMetadata? metadata)) {
            Logger.Error("Invalid skill use: {SkillId},{Level}", skillId, level);
            return;
        }

        var record = new SkillRecord(metadata, skillUid, session.Player.ObjectId) {ServerTick = serverTick};
        byte motionPoint = packet.ReadByte();
        if (!record.TrySetMotionPoint(motionPoint)) {
            Logger.Error("Invalid MotionPoint({MotionPoint}) for {Record}", motionPoint, record);
            return;
        }

        record.Position = packet.Read<Vector3>();
        record.Direction = packet.Read<Vector3>();
        record.Rotation = packet.Read<Vector3>();
        record.Rotate2Z = packet.ReadFloat(); // Rotation2Z

        packet.ReadInt(); // ClientTick
        record.Unknown = packet.ReadBool(); // UnkBool
        packet.ReadLong(); // UnkLong
        try {
            record.IsHold = packet.ReadBool();
            if (record.IsHold) {
                record.HoldInt = packet.ReadInt();
                record.HoldString = packet.ReadUnicodeString();
            }
        } catch { /* Ignored */ }

        session.Player.InBattle = true;
        session.Skill = record;
        session.Field?.Broadcast(SkillPacket.Use(record));

        foreach (SkillEffectMetadata effect in metadata.Data.Skills) {
            session.Player.ApplyEffect(session.Player, effect);
        }
    }

    private void HandlePoint(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        if (session.Skill?.Uid != skillUid) {
            Logger.Warning("SkillUid mismatch {Existing} != {PointCast}", session.Skill?.Uid, skillUid);
            return;
        }

        byte attackPoint = packet.ReadByte();
        if (!session.Skill.TrySetAttackPoint(attackPoint)) {
            Logger.Error("Invalid AttackPoint({AttackPoint}) for {Record}", attackPoint, session.Skill);
            return;
        }

        session.Skill.Position = packet.Read<Vector3>();
        session.Skill.Direction = packet.Read<Vector3>();

        byte count = packet.ReadByte();
        int unknown = packet.ReadInt(); // Unknown(0)
        if (unknown != 0) {
            Logger.Error("Unhandled skill-Point value: {Record}", session.Skill);
        }
        TargetRecord[] targets = packet.ReadArray<TargetRecord>(count);

        session.Player.InBattle = true;
        session.Field?.Broadcast(SkillDamagePacket.Target(session.Skill, targets), session);
    }

    private void HandleTarget(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        if (session.Skill?.Uid != skillUid) {
            Logger.Warning("SkillUid mismatch {Existing} != {TargetCast}", session.Skill?.Uid, skillUid);
            return;
        }

        int attackCounter = packet.ReadInt();
        int casterId = packet.ReadInt(); // Unknown(0)
        if (casterId != 0 && casterId != session.Player.ObjectId) {
            Logger.Error("Unhandled skill-Target value1({Value}): {Record}", casterId, session.Skill);
        }

        packet.Read<Vector3>();
        packet.Read<Vector3>();
        session.Skill.Direction = packet.Read<Vector3>();
        byte attackPoint = packet.ReadByte();
        if (!session.Skill.TrySetAttackPoint(attackPoint)) {
            Logger.Error("Invalid AttackPoint({AttackPoint}) for {Record}", attackPoint, session.Skill);
            return;
        }

        byte count = packet.ReadByte();
        if (count > session.Skill.Attack.TargetCount) {
            Logger.Error("Attack too many targets {Count} for {Record}", count, session.Skill);
            return;
        }

        int unknown2 = packet.ReadInt(); // Unknown(0)
        if (unknown2 != 0) {
            Logger.Error("Unhandled skill-Target value2({Value}): {Record}", unknown2, session.Skill);
        }

        int[] targetIds = new int[count];
        for (byte i = 0; i < count; i++) {
            targetIds[i] = packet.ReadInt();
            packet.ReadByte();
        }

        session.Player.TargetAttack(session.Skill, targetIds);
        session.Player.InBattle = true;
    }

    private void HandleSplash(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        if (session.Skill?.Uid != skillUid) {
            Logger.Warning("SkillUid mismatch {Existing} != {MagicPathCast}", session.Skill?.Uid, skillUid);
            return;
        }

        byte attackPoint = packet.ReadByte();
        if (!session.Skill.TrySetAttackPoint(attackPoint)) {
            Logger.Error("Invalid AttackPoint({AttackPoint}) for {Record}", attackPoint, session.Skill);
            return;
        }

        int unknown1 = packet.ReadInt(); // Unknown(0)
        if (unknown1 != 0) {
            Logger.Error("Unhandled skill-MagicPath value1({Value}): {Record}", unknown1, session.Skill);
        }

        int unknown2 = packet.ReadInt(); // Unknown(0)
        if (unknown2 != 0) {
            Logger.Error("Unhandled skill-MagicPath value2({Value}): {Record}", unknown2, session.Skill);
        }

        session.Skill.Position = packet.Read<Vector3>();
        session.Skill.Rotation = packet.Read<Vector3>();

        SkillMetadataAttack attack = session.Skill.Attack;
        if (attack.CubeMagicPathId == 0) {
            Logger.Error("Invalid skill-MagicPath({CubeMagicPath}) for {Record}", attack.CubeMagicPathId, session.Skill);
            return;
        }
        if (!TableMetadata.MagicPathTable.Entries.TryGetValue(attack.CubeMagicPathId, out IReadOnlyList<MagicPath>? magicPaths)) {
            Logger.Error("No MagicPath found for {CubeMagicPath})", attack.CubeMagicPathId);
            return;
        }

        session.Player.SplashAttack(session.Skill, magicPaths);
        session.Player.InBattle = true;
    }

    private void HandleSync(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        if (session.Skill?.Uid != skillUid) {
            Logger.Warning("SkillUid mismatch {Existing} != {SyncCast}", session.Skill?.Uid, skillUid);
            return;
        }

        session.Field?.Broadcast(SkillPacket.Sync(session.Skill), session);
    }

    private void HandleTickSync(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        if (session.Skill?.Uid != skillUid) {
            Logger.Warning("SkillUid mismatch {Existing} != {TickSyncCast}", session.Skill?.Uid, skillUid);
            return;
        }

        session.Skill.ServerTick = packet.ReadInt();
    }

    private void HandleCancel(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        SkillRecord? record = session.Skill;
        if (record?.Uid != skillUid) {
            Logger.Warning("SkillUid mismatch {Existing} != {CancelCast}", session.Skill?.Uid, skillUid);
            return;
        }

        session.Player.InBattle = true;
        session.Skill = null;
        session.Field?.Broadcast(SkillPacket.Cancel(record), session);
    }
}
