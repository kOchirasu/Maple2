﻿using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model.Skill;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class SkillDamagePacket {
    private enum Command : byte {
        Target = 0,
        Damage = 1,
        DotDamage = 3,
        Heal = 4,
        Region = 5,
        Tile = 6,
        Unknown7 = 7,
        Unknown8 = 8,
    }

    public static ByteWriter Target(SkillRecord record, ICollection<TargetRecord> targets) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Target);
        pWriter.WriteLong(record.CastUid);
        pWriter.WriteInt(record.Caster.ObjectId);
        pWriter.WriteInt(record.SkillId);
        pWriter.WriteShort(record.Level);
        pWriter.WriteByte(record.MotionPoint);
        pWriter.WriteByte(record.AttackPoint);
        pWriter.Write<Vector3S>(record.Position); // Impact
        pWriter.Write<Vector3>(record.Direction); // Impact
        pWriter.WriteBool(false); // SkillId:10600211 only
        pWriter.WriteInt(record.ServerTick);

        pWriter.WriteByte((byte) targets.Count);
        foreach (TargetRecord target in targets) {
            pWriter.WriteClass<TargetRecord>(target);
        }

        return pWriter;
    }

    public static ByteWriter Damage(DamageRecord record) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Damage);
        pWriter.WriteLong(record.SkillUid);
        pWriter.WriteLong(record.TargetUid);
        pWriter.WriteInt(record.CasterId);
        pWriter.WriteInt(record.SkillId);
        pWriter.WriteShort(record.Level);
        pWriter.WriteByte(record.MotionPoint);
        pWriter.WriteByte(record.AttackPoint);
        pWriter.Write<Vector3S>(record.Position);  // Impact
        pWriter.Write<Vector3S>(record.Direction);

        pWriter.WriteByte((byte) record.Targets.Count);
        foreach (DamageRecordTarget target in record.Targets) {
            pWriter.WriteInt(target.ObjectId);
            pWriter.WriteByte((byte) target.Damage.Count);
            foreach ((DamageType type, long amount) in target.Damage) {
                pWriter.Write<DamageType>(type);
                pWriter.WriteLong(amount);
            }
        }

        return pWriter;
    }

    public static ByteWriter DotDamage(DotDamageRecord record) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.DotDamage);
        pWriter.WriteInt(record.Caster.ObjectId);
        pWriter.WriteInt(record.Target.ObjectId);
        pWriter.WriteInt(record.ProcCount);
        pWriter.Write<DamageType>(record.Type);
        pWriter.WriteInt(record.HpAmount);

        return pWriter;
    }

    public static ByteWriter Heal(HealDamageRecord record, bool animate = true) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Heal);
        pWriter.WriteClass<HealDamageRecord>(record);
        pWriter.WriteBool(animate);

        return pWriter;
    }

    public static ByteWriter Region(DamageRecord record) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Region);
        pWriter.WriteLong(record.SkillUid);
        pWriter.WriteInt(record.CasterId);
        pWriter.WriteInt(record.OwnerId);
        pWriter.WriteByte(record.AttackPoint);

        pWriter.WriteByte((byte) record.Targets.Count);
        foreach (DamageRecordTarget target in record.Targets) {
            pWriter.WriteInt(target.ObjectId);
            pWriter.WriteByte((byte) target.Damage.Count);
            pWriter.Write<Vector3S>(target.Position); // Of Block
            pWriter.Write<Vector3>(target.Direction);
            foreach ((DamageType type, long amount) in target.Damage) {
                pWriter.Write<DamageType>(type);
                pWriter.WriteLong(amount);
            }
        }

        return pWriter;
    }

    public static ByteWriter Tile(DamageRecord record) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Tile);
        pWriter.WriteLong(record.SkillUid);
        pWriter.WriteInt(record.SkillId);
        pWriter.WriteShort(record.Level);

        pWriter.WriteByte((byte) record.Targets.Count);
        foreach (DamageRecordTarget target in record.Targets) {
            pWriter.WriteInt(target.ObjectId);
            pWriter.WriteByte((byte) target.Damage.Count);
            pWriter.Write<Vector3S>(target.Position); // Of Block
            pWriter.Write<Vector3>(target.Direction);
            foreach ((DamageType type, long amount) in target.Damage) {
                pWriter.Write<DamageType>(type);
                pWriter.WriteLong(amount);
            }
        }

        return pWriter;
    }

    public static ByteWriter Unknown7() {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Unknown7);

        return pWriter;
    }

    public static ByteWriter Target() {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Unknown8);

        return pWriter;
    }
}
