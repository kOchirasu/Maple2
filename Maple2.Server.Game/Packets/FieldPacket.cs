using System;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Core.Packets.Helper;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class FieldPacket {
    public static ByteWriter AddPlayer(GameSession session) {
        Player player = session.Player;

        var pWriter = Packet.Of(SendOp.FIELD_ADD_USER);
        pWriter.WriteInt(session.Player.ObjectId);
        pWriter.WriteCharacter(player.Account, player.Character);
        pWriter.WriteClass<JobInfo>(session.Skill.JobInfo);
        pWriter.Write<Vector3>(session.Player.Position);
        pWriter.Write<Vector3>(session.Player.Rotation);
        pWriter.WriteByte();
        // Stats
        pWriter.WriteBool(false); // InBattle
        // ...
        pWriter.Write<SkinColor>(player.Character.SkinColor);
        pWriter.WriteUnicodeString(player.Character.Picture);
        // Mount
        pWriter.WriteInt();
        pWriter.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // ???
        pWriter.WriteInt(); // Weekly Architect Score
        pWriter.WriteInt(); // Architect Score
        // Equips
        // Buffs
        // ...

        return pWriter;
    }
}
