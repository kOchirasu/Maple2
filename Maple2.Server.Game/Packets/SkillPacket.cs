using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Model.Skill;

namespace Maple2.Server.Game.Packets;

public static class SkillPacket {
    public static ByteWriter Use(IActor owner, SkillRecord skill) {
        var pWriter = Packet.Of(SendOp.SkillUse);

        return pWriter;
    }

    public static ByteWriter Sync(IActor owner, SkillRecord skill) {
        var pWriter = Packet.Of(SendOp.SkillSync);

        return pWriter;
    }

    public static ByteWriter Cancel(SkillRecord skill) {
        var pWriter = Packet.Of(SendOp.SkillCancel);
        pWriter.WriteLong(skill.Uid);
        pWriter.WriteInt(skill.CasterId);

        return pWriter;
    }

    public static ByteWriter Fail(SkillRecord skill) {
        var pWriter = Packet.Of(SendOp.SkillUseFailed);
        pWriter.WriteByte(); // Type?
        pWriter.WriteLong(skill.Uid);
        pWriter.WriteInt(skill.CasterId);
        pWriter.WriteByte(); // Unknown

        // TODO: Handle Type=5, Type=6

        pWriter.WriteInt();         // EndTick?
        pWriter.WriteShort(0 * 10); // Unk * 10
        pWriter.WriteInt();         // Same CooldownUnk in SkillCooldown packet?

        return pWriter;
    }

    public static ByteWriter StateSkill(FieldPlayer player, int skillId, long skillCastUid) {
        var pWriter = Packet.Of(SendOp.StateSkill);
        pWriter.WriteByte(0x06);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteInt(); // ItemId??
        pWriter.WriteLong(skillCastUid);
        pWriter.WriteInt(skillId);
        pWriter.WriteInt((int) player.State);

        return pWriter;
    }

    public static ByteWriter Cooldown() {
        var pWriter = Packet.Of(SendOp.SkillCooldown);
        pWriter.WriteByte(0); // Count
        // [4] SkillId
        // [4] EndTick
        // [4] CooldownUnk

        return pWriter;
    }

    public static ByteWriter ResetCooldown() {
        var pWriter = Packet.Of(SendOp.SkillResetCooldown);

        return pWriter;
    }
}
