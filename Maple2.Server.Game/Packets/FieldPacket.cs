using System;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Core.Packets.Helper;
using Maple2.Server.Game.Model;
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

        // TODO: Stats

        pWriter.WriteBool(session.Player.InBattle);

        #region Unknown Cube Section
        pWriter.WriteByte();
        #region CubeItemInfo
        pWriter.WriteInt(); // ItemId
        pWriter.WriteLong(); // ItemUid
        pWriter.WriteLong(); // Unknown
        pWriter.WriteBool(false); // IsUgc
        //pWriter.WriteClass<UgcItemLook>(...);
        #endregion
        pWriter.WriteInt();
        #endregion

        pWriter.Write<SkinColor>(player.Character.SkinColor);
        pWriter.WriteUnicodeString(player.Character.Picture);
        pWriter.WriteBool(false); // TODO: Mount
        pWriter.WriteInt();
        pWriter.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // ???
        pWriter.WriteInt(); // Weekly Architect Score
        pWriter.WriteInt(); // Architect Score

        using (var buffer = new PoolByteWriter()) {
            int count = session.Item.Equips.Gear.Count + session.Item.Equips.Outfit.Count;
            buffer.WriteByte((byte) count);
            foreach (Item item in session.Item.Equips.Gear.Values) {
                buffer.WriteEquip(item);
            }
            foreach (Item item in session.Item.Equips.Outfit.Values) {
                buffer.WriteEquip(item);
            }
            // Don't know...
            buffer.WriteBool(true);
            buffer.WriteLong();
            buffer.WriteLong();
            // Outfit2
            buffer.WriteByte(0);

            pWriter.WriteDeflated(buffer.Buffer, 0, buffer.Length);
        }

        using (var buffer = new PoolByteWriter()) {
            buffer.WriteByte(0); // Unknown

            pWriter.WriteDeflated(buffer.Buffer, 0, buffer.Length);
        }

        using (var buffer = new PoolByteWriter()) {
            buffer.WriteByte((byte) session.Item.Equips.Badge.Count);
            foreach (Item item in session.Item.Equips.Badge.Values) {
                buffer.WriteBadge(item);
            }

            pWriter.WriteDeflated(buffer.Buffer, 0, buffer.Length);
        }

        pWriter.WriteShort((short) session.Player.Buffs.Count);
        foreach (Buff buff in session.Player.Buffs.Values) {
            pWriter.WriteClass<Buff>(buff);
        }

        #region sub_BF6440
        pWriter.WriteInt();
        pWriter.WriteInt();
        #endregion

        pWriter.WriteByte();
        pWriter.WriteInt(player.Character.Title);
        pWriter.WriteShort(player.Character.Insignia);
        pWriter.WriteByte(); // InsigniaValue

        pWriter.WriteInt();
        pWriter.WriteBool(false); // TODO: Pet
        pWriter.WriteLong(player.Account.PremiumTime);
        pWriter.WriteInt();
        pWriter.WriteByte();
        pWriter.WriteInt(); // Tail

        return pWriter;
    }
}
