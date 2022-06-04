using System;
using System.Numerics;
using Maple2.Database.Extensions;
using Maple2.Model;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Core.Packets.Helper;
using Maple2.Server.Game.Manager.Config;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class FieldPacket {
    public static ByteWriter AddPlayer(GameSession session) {
        Player player = session.Player;

        var pWriter = Packet.Of(SendOp.FIELD_ADD_USER);
        pWriter.WriteInt(session.Player.ObjectId);
        pWriter.WriteCharacter(session);
        pWriter.WriteClass<SkillInfo>(session.Config.Skill.SkillInfo);
        pWriter.Write<Vector3>(session.Player.Position);
        pWriter.Write<Vector3>(session.Player.Rotation);
        pWriter.WriteByte();
        pWriter.WritePlayerStats(session.Player.Stats);
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

        #region sub_5F1C30
        pWriter.WriteInt();
        pWriter.WriteByte();
        pWriter.WriteByte();
        #endregion

        pWriter.WriteInt(player.Character.Title);
        pWriter.WriteShort(player.Character.Insignia);
        pWriter.WriteByte(); // InsigniaValue

        pWriter.WriteInt();
        pWriter.WriteBool(false); // TODO: Pet
        pWriter.WriteLong(player.Account.PremiumTime);
        pWriter.WriteInt();
        pWriter.WriteByte();
        pWriter.WriteInt(); // Tail
        pWriter.WriteInt();
        pWriter.WriteShort();

        return pWriter;
    }

    public static ByteWriter RemovePlayer(int objectId) {
        var pWriter = Packet.Of(SendOp.FIELD_REMOVE_USER);
        pWriter.WriteInt(objectId);

        return pWriter;
    }

    public static ByteWriter DropItem(FieldEntity<Item> fieldItem) {
        Item item = fieldItem;

        var pWriter = Packet.Of(SendOp.FIELD_ADD_ITEM);
        pWriter.WriteInt(fieldItem.ObjectId);
        pWriter.WriteInt(item.Id);
        pWriter.WriteInt(item.Amount);

        pWriter.WriteBool(item.Uid >= 0);
        if (item.Uid >= 0) {
            pWriter.WriteLong(item.Uid);
        }

        pWriter.Write<Vector3>(fieldItem.Position);
        pWriter.WriteInt(fieldItem.Owner?.ObjectId ?? 0);
        pWriter.WriteInt();
        pWriter.WriteByte(2); // Required for item to show up
        pWriter.WriteInt(item.Rarity);
        pWriter.WriteShort();
        pWriter.WriteBool(false);
        pWriter.WriteBool(false);

        if (!item.IsMeso()) {
            pWriter.WriteClass<Item>(item);
        }

        return pWriter;
    }

    public static ByteWriter RemoveItem(int objectId) {
        var pWriter = Packet.Of(SendOp.FIELD_REMOVE_ITEM);
        pWriter.WriteInt(objectId);

        return pWriter;
    }

    private static void WriteCharacter(this IByteWriter writer, GameSession session) {
        Account account = session.Player.Value.Account;
        Character character = session.Player.Value.Character;
        writer.WriteLong(account.Id);
        writer.WriteLong(character.Id);
        writer.WriteUnicodeString(character.Name);
        writer.Write<Gender>(character.Gender);
        writer.WriteByte(1);
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt(character.MapId);
        writer.WriteInt(character.InstanceMapId);
        writer.WriteInt(character.InstanceId);
        writer.WriteShort(character.Level);
        writer.WriteShort(character.Channel);
        writer.WriteInt((int)character.Job.Code());
        writer.Write<Job>(character.Job);
        writer.WriteInt((int) session.Stats.Values[StatAttribute.Health].Current);
        writer.WriteInt((int) session.Stats.Values[StatAttribute.Health].Total);
        writer.WriteShort();
        writer.WriteLong();
        writer.WriteLong(character.StorageCooldown);
        writer.WriteLong(character.DoctorCooldown);
        writer.WriteInt(character.ReturnMapId);
        writer.Write<Vector3>(character.ReturnPosition);
        writer.WriteInt(session.Stats.Values.GearScore);
        writer.Write<SkinColor>(character.SkinColor);
        writer.WriteLong(character.CreationTime);
        writer.Write<Trophy>(account.Trophy);
        writer.WriteLong(); // GuildId
        writer.WriteUnicodeString(); // GuildName
        writer.WriteUnicodeString(character.Motto);
        writer.WriteUnicodeString(character.Picture);
        writer.WriteByte(); // Club Count
        writer.WriteByte(); // PCBang related?
        writer.Write<Mastery>(character.Mastery);
        #region Unknown
        writer.WriteUnicodeString();
        writer.WriteLong();
        writer.WriteLong();
        writer.WriteLong();
        #endregion
        writer.WriteInt(); // Unknown Count
        writer.WriteByte();
        writer.WriteBool(false);
        writer.WriteLong(); // Birthday
        writer.WriteInt();
        writer.WriteInt();
        writer.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        writer.WriteInt(account.PrestigeLevel); // PrestigeLevel
        writer.WriteLong(character.LastModified.ToEpochSeconds());
        writer.WriteInt(); // Unknown Count
        writer.WriteInt(); // Unknown Count
        writer.WriteShort(); // Survival related?
        writer.WriteLong();
    }
}
