using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

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
        pWriter.WriteLong(record.Uid);
        pWriter.WriteInt(record.CasterId);
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
            pWriter.WriteLong();
            pWriter.Write<TargetRecord>(target);
        }

        return pWriter;
    }

    public static ByteWriter Damage(DamageRecord record) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Damage);
        pWriter.WriteLong(record.Uid);
        pWriter.WriteInt(record.AttackCounter); // AttackCounter
        pWriter.WriteInt(record.CasterId);
        pWriter.WriteInt(record.OwnerId);
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
        pWriter.WriteInt(record.OwnerId);
        pWriter.WriteInt(record.TargetId);
        pWriter.WriteInt(record.Count);
        pWriter.Write<DamageType>(record.Type);
        pWriter.WriteLong(record.Amount);

        return pWriter;
    }

    public static ByteWriter Heal(HealDamageRecord record) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Heal);
        pWriter.WriteInt(record.OwnerId);
        pWriter.WriteInt(record.TargetId);
        pWriter.WriteInt(record.Count);
        pWriter.WriteLong(record.HpAmount);
        pWriter.WriteInt(record.SpAmount);
        pWriter.WriteByte(record.EpAmount);

        return pWriter;
    }

    public static ByteWriter Region(DamageRecord record) {
        var pWriter = Packet.Of(SendOp.SkillDamage);
        pWriter.Write<Command>(Command.Region);
        pWriter.WriteLong(record.Uid);
        pWriter.WriteInt(record.CasterId);
        pWriter.WriteInt(record.OwnerId);

        pWriter.WriteByte((byte) record.Targets.Count);
        foreach (DamageRecordTarget target in record.Targets) {
            pWriter.WriteInt(target.ObjectId);
            pWriter.WriteByte((byte) target.Damage.Count);
            pWriter.Write<Vector3>(record.Position); // Of Block
            pWriter.Write<Vector3>(record.Direction);
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
        pWriter.WriteLong(record.Uid);
        pWriter.WriteInt(record.SkillId);
        pWriter.WriteShort(record.Level);

        pWriter.WriteByte((byte) record.Targets.Count);
        foreach (DamageRecordTarget target in record.Targets) {
            pWriter.WriteInt(target.ObjectId);
            pWriter.WriteByte((byte) target.Damage.Count);
            pWriter.Write<Vector3>(record.Position); // Of Block
            pWriter.Write<Vector3>(record.Direction);
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
