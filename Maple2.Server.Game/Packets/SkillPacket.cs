using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;

namespace Maple2.Server.Game.Packets;

public static class SkillPacket {
    public static ByteWriter StateSkill(FieldPlayer player, int skillId, long skillCastUid) {
        var pWriter = Packet.Of(SendOp.STATE_SKILL);
        pWriter.WriteByte(0x06);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteInt(); // ItemId??
        pWriter.WriteLong(skillCastUid);
        pWriter.WriteInt(skillId);
        pWriter.WriteInt((int) player.State);

        return pWriter;
    }
}
