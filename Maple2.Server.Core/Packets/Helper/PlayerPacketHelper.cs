using System;
using System.Collections.Generic;
using System.Numerics;
using Maple2.Database.Extensions;
using Maple2.Model;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Core.Packets.Helper;

public static class PlayerPacketHelper {
    public static void WriteCharacter(this IByteWriter writer, Account account, Character character) {
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
        writer.WriteInt(); // CurrentHp
        writer.WriteInt(); // MaxHp
        writer.WriteShort();
        writer.WriteLong();
        writer.WriteLong(character.StorageCooldown);
        writer.WriteLong(character.DoctorCooldown);
        writer.WriteInt(character.ReturnMapId);
        writer.Write<Vector3>(character.ReturnPosition);
        writer.WriteInt(); // GearScore
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

    public static void WriteEquip(this IByteWriter writer, Item equip) {
        writer.WriteInt(equip.Id);
        writer.WriteLong(equip.Uid);
        writer.WriteUnicodeString(equip.EquipSlot.ToString());
        writer.WriteInt(equip.Rarity);
        writer.WriteClass<Item>(equip);
    }

    public static void WriteBadge(this IByteWriter writer, Item badge) {
        if (badge.Badge == null) {
            throw new ArgumentNullException(nameof(badge.Badge));
        }

        writer.Write<BadgeType>(badge.Badge.Type);
        writer.WriteInt(badge.Id);
        writer.WriteLong(badge.Uid);
        writer.WriteInt(badge.Rarity);
        writer.WriteClass<Item>(badge);
    }
}
