﻿using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Model.Enum;
using Maple2.Server.Game.Model.Field.Actor.ActorStateComponent;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

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
    public required SkillMetadataStorage SkillMetadata { private get; init; }
    public required TableMetadataStorage TableMetadata { private get; init; }
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

        if (session.HeldLiftup != null) {
            if (session.HeldLiftup.SkillId == skillId && session.HeldLiftup.Level == level) {
                session.HeldLiftup = null;
            } else {
                // Cannot use other skills while holding LiftupWeapon.
                return;
            }
        }

        if (!SkillMetadata.TryGet(skillId, level, out SkillMetadata? metadata)) {
            Logger.Error("Invalid skill use: {SkillId},{Level}", skillId, level);
            return;
        }

        var record = new SkillRecord(metadata, skillUid, session.Player) { ServerTick = serverTick };
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
        long itemUid = packet.ReadLong();
        record.IsHold = packet.ReadBool();
        if (record.IsHold) {
            record.HoldInt = packet.ReadInt();
            record.HoldString = packet.ReadUnicodeString();

            if (session.Player.DebugSkills) {
                session.Send(NoticePacket.Message($"Skill.Use: {skillId}, {skillUid}; IsHold: true; HoldInt: {record.HoldInt}; HoldString: {record.HoldString}; UnkBool: {record.Unknown}"));
            }
        } else if (session.Player.DebugSkills) {
            session.Send(NoticePacket.Message($"Skill.Use: {skillId}, {skillUid}; IsHold: false; UnkBool: {record.Unknown}"));
        }

        if (itemUid > 0) {
            Item? item = session.Item.Inventory.Get(itemUid);
            // TODO: Check if item is valid for skill?
            if (item == null || !session.Item.Inventory.Consume(item.Uid, 1)) {
                session.Send(NoticePacket.Notice(NoticePacket.Flags.Alert, StringCode.s_err_invalid_item));
                return;
            }
        }

        //TODO: Proper invoke cost
        /*
        InvokeStatValue invokeStat = Stats.GetSkillStats(skillCast.SkillId, skillCast.GetSkillGroups(), InvokeEffectType.ReduceSpiritCost);
        spiritCost = Math.Max(0, (int) (-invokeStat.Value + (1 - invokeStat.Rate) * spiritCost));
        */
        if (metadata.Data.Consume.Stat.TryGetValue(BasicAttribute.Spirit, out long spiritCost)) {
            if (session.Player.Stats[BasicAttribute.Spirit].Current < spiritCost) {
                Logger.Error("Not enough spirit to cast skill: {SkillId},{Level}", skillId, level);
                return;
            }
        }

        if (metadata.Data.Consume.Stat.TryGetValue(BasicAttribute.Stamina, out long staminaCost)) {
            if (session.Player.Stats[BasicAttribute.Stamina].Current < staminaCost) {
                Logger.Error("Not enough stamina to cast skill: {SkillId},{Level}", skillId, level);
                return;
            }
        }

        session.Player.ConsumeSp((int) spiritCost);
        session.Player.ConsumeStamina((int) staminaCost);
        session.Field.Broadcast(StatsPacket.Update(session.Player, [BasicAttribute.Spirit, BasicAttribute.Stamina]));

        session.ConditionUpdate(ConditionType.skill, 1, codeLong: skillId);

        session.Player.InBattle = itemUid <= 0;
        session.ActiveSkills.Add(record);
        session.Field?.Broadcast(SkillPacket.Use(record));

        SkillMetadataMotionProperty motion = metadata.Data.Motions[0].MotionProperty;

        session.Player.AnimationState.TryPlaySequence(motion.SequenceName, motion.SequenceSpeed, AnimationType.Skill);

        foreach (SkillEffectMetadata effect in metadata.Data.Skills) {
            session.Player.ApplyEffect(session.Player, session.Player, effect);
        }

        session.Config.SaveSkillCooldown(metadata);
    }

    private void HandlePoint(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        SkillRecord? record = session.ActiveSkills.Get(skillUid);
        if (record == null) {
            Logger.Warning("Invalid Attack-Point Skill {SkillUid}", skillUid);
            return;
        }

        byte attackPoint = packet.ReadByte();
        if (!record.TrySetAttackPoint(attackPoint)) {
            Logger.Error("Invalid AttackPoint({AttackPoint}) for {Record}", attackPoint, record);
            return;
        }

        record.Position = packet.Read<Vector3>();
        record.Direction = packet.Read<Vector3>();

        if (session.Player.DebugSkills) {
            session.Send(NoticePacket.Message($"Skill.Attack.Point: {skillUid}; AttackPoint: {attackPoint}"));
        }

        byte count = packet.ReadByte();
        // Note: counts up for skills that are held down (reused skillUid)
        int iterations = packet.ReadInt();

        for (byte i = 0; i < count; i++) {
            var targets = new List<TargetRecord>();
            var targetRecord = new TargetRecord {
                Uid = packet.ReadLong(),
                TargetId = packet.ReadInt(),
                Unknown = packet.ReadByte(),
            };
            targets.Add(targetRecord);

            // While more targets in packet.
            while (packet.ReadBool()) {
                targetRecord = new TargetRecord {
                    PrevUid = targetRecord.Uid,
                    Uid = packet.ReadLong(),
                    TargetId = packet.ReadInt(),
                    Unknown = packet.ReadByte(),
                    Index = packet.ReadByte(),
                };
                targets.Add(targetRecord);
            }

            session.Player.InBattle = true;
            session.Field?.Broadcast(SkillDamagePacket.Target(record, targets), session);
        }
    }

    private void HandleTarget(GameSession session, IByteReader packet) {
        if (session.Field == null) {
            return;
        }

        long skillUid = packet.ReadLong();
        SkillRecord? record = session.ActiveSkills.Get(skillUid);
        if (record == null) {
            Logger.Warning("Invalid Attack-Target Skill {SkillUid}", skillUid);
            return;
        }

        record.TargetUid = packet.ReadLong();
        record.ImpactPosition = packet.Read<Vector3>();
        packet.Read<Vector3>(); // ImpactPosition2
        record.Direction = packet.Read<Vector3>();
        byte attackPoint = packet.ReadByte();
        if (!record.TrySetAttackPoint(attackPoint)) {
            Logger.Error("Invalid AttackPoint({AttackPoint}) for {Record}", attackPoint, record);
            return;
        }

        byte count = packet.ReadByte();
        if (count > record.Attack.TargetCount) {
            Logger.Error("Attack too many targets {Count} for {Record}", count, record);
            return;
        }

        int unknown2 = packet.ReadInt(); // Unknown(0)
        if (unknown2 != 0) {
            Logger.Error("Unhandled skill-Target value2({Value}): {Record}", unknown2, record);
        }

        if (session.Player.DebugSkills) {
            session.Send(NoticePacket.Message($"Skill.Attack.Damage: {skillUid}; AttackPoint: {attackPoint}"));
        }

        for (byte i = 0; i < count; i++) {
            int targetId = packet.ReadInt();
            packet.ReadByte();

            switch (record.Attack.Range.ApplyTarget) {
                case SkillEntity.Target:
                    if (session.Field.Mobs.TryGetValue(targetId, out FieldNpc? npc)) {
                        record.Targets.Add(npc);
                    }
                    continue;
                case SkillEntity.Owner:
                    if (session.Field.TryGetPlayer(targetId, out FieldPlayer? player)) {
                        record.Targets.Add(player);
                    }
                    continue;
                case SkillEntity.RegionPet:
                    if (session.Field.Pets.TryGetValue(targetId, out FieldPet? pet)) {
                        record.Targets.Add(pet);
                    }
                    continue;
                default:
                    Logger.Debug("Unhandled Target-SkillEntity:{Entity}", record.Attack.Range.ApplyTarget);
                    continue;
            }
        }

        session.Player.TargetAttack(record);
        session.Player.InBattle = true;
    }

    private void HandleSplash(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        SkillRecord? record = session.ActiveSkills.Get(skillUid);
        if (record == null) {
            Logger.Warning("Invalid Attack-Splash Skill {SkillUid}", skillUid);
            return;
        }

        byte attackPoint = packet.ReadByte();
        if (!record.TrySetAttackPoint(attackPoint)) {
            Logger.Error("Invalid AttackPoint({AttackPoint}) for {Record}", attackPoint, record);
            return;
        }

        int unknown1 = packet.ReadInt(); // Unknown(0)
        if (unknown1 != 0) {
            Logger.Error("Unhandled skill-MagicPath value1({Value}): {Record}", unknown1, record);
        }

        int unknown2 = packet.ReadInt(); // Unknown(0)
        if (unknown2 != 0) {
            Logger.Error("Unhandled skill-MagicPath value2({Value}): {Record}", unknown2, record);
        }

        if (session.Player.DebugSkills) {
            session.Send(NoticePacket.Message($"Skill.Attack.Region: {skillUid}; AttackPoint: {attackPoint}; UnkInt: {unknown1}; UnkInt: {unknown2}"));
        }

        record.Position = packet.Read<Vector3>();
        record.Rotation = packet.Read<Vector3>();

        session.Field?.AddSkill(record);
        session.Player.InBattle = true;
    }

    private void HandleSync(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        SkillRecord? record = session.ActiveSkills.Get(skillUid);
        if (record == null) {
            Logger.Warning("Invalid Sync Skill {SkillUid}", skillUid);
            return;
        }
        int skillId = packet.ReadInt();
        short skillLevel = packet.ReadShort();
        byte motionPoint = packet.ReadByte();

        if (record.Metadata.Data.Motions.Length >= motionPoint) {
            Logger.Warning($"Invalid motion point {motionPoint} for {record}", skillUid);
            return;
        }

        if (session.Player.AnimationState.PlayingSequence is null) {
            Logger.Warning($"Last motion already expired on skill {skillUid}");
        }

        record.TrySetMotionPoint(motionPoint);

        Vector3 position = packet.Read<Vector3>();
        Vector3 unk = packet.Read<Vector3>(); // either velocity or direction
        Vector3 rotation = packet.Read<Vector3>();
        Vector3 input = packet.Read<Vector3>(); // x and y match with wasd/arrow input, not normalized
        bool toggle = packet.ReadByte() == 1;
        int unk3 = packet.ReadInt();
        byte unk4 = packet.ReadByte();

        session.Field?.Broadcast(SkillPacket.Sync(record), session);

        if (session.Player.DebugSkills) {
            session.Send(NoticePacket.Message($"Skill.Sync: {skillId},{skillUid}; AttackPoint: {motionPoint}; Toggle: {toggle}; UnkInt: {unk3}; UnkByte: {unk4}; UnkVec: {unk}"));
        }

        SkillMetadataMotionProperty motion = record.Metadata.Data.Motions[motionPoint].MotionProperty;

        session.Player.AnimationState.TryPlaySequence(motion.SequenceName, motion.SequenceSpeed, AnimationType.Skill);
    }

    private void HandleTickSync(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        SkillRecord? record = session.ActiveSkills.Get(skillUid);
        if (record == null) {
            Logger.Warning("Invalid TickSync Skill {SkillUid}", skillUid);
            return;
        }

        record.ServerTick = packet.ReadInt();

        if (session.Player.DebugSkills) {
            session.Send(NoticePacket.Message($"Skill.SyncTick: {skillUid}"));
        }

        string skillSequence = record.Motion.MotionProperty.SequenceName;
        string playingSequence = session.Player.AnimationState.PlayingSequence?.Name ?? "";

        if (skillSequence != playingSequence) {
            Logger.Warning($"Motion point on skill cast {skillUid} '{skillSequence}' doesn't match playing sequence '{playingSequence}'", skillUid);
            return;
        }

        session.Player.AnimationState.SetLoopSequence(true, true);
    }

    private void HandleCancel(GameSession session, IByteReader packet) {
        long skillUid = packet.ReadLong();
        SkillRecord? record = session.ActiveSkills.Get(skillUid);
        if (record == null) {
            Logger.Warning("Invalid Cancel Skill {SkillUid}", skillUid);
            return;
        }

        session.Player.InBattle = true;
        session.Field?.Broadcast(SkillPacket.Cancel(record), session);

        if (session.Player.DebugSkills) {
            session.Send(NoticePacket.Message($"Skill.Cancel: {skillUid}"));
        }

        session.Player.AnimationState.CancelSequence();
    }
}
