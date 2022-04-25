using System;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class Character : IByteSerializable {
    #region Immutable
    public long CreationTime { get; init; }
    public long LastModified { get; init; }

    public long Id { get; init; }
    public long AccountId { get; init; }
    
    public Account Account { get; init; }
    #endregion

    public DateTimeOffset DeleteTime;

    public string Name;
    public Gender Gender;
    public int MapId;
    public JobCode JobCode => (JobCode)((int)Job / 10);
    public Job Job;
    
    public SkinColor SkinColor;
    public short Level;
    public long Exp;
    public long RestExp;
    
    public int Title;
    public short Insignia;

    public int InstanceId;
    public int InstanceMapId;
    public short Channel;
    
    public long StorageCooldown => DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
    public long DoctorCooldown => DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();

    public int ReturnMapId;
    public Vector3 ReturnPosition;
    public Trophy Trophy;
    public string Motto;
    public string Picture;
    public Mastery Mastery;

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong(AccountId);
        writer.WriteLong(Id);
        writer.WriteUnicodeString(Name);
        writer.Write<Gender>(Gender);
        writer.WriteByte(1);
        writer.WriteLong();
        writer.WriteInt();
        writer.WriteInt(MapId);
        writer.WriteInt(InstanceMapId);
        writer.WriteInt(InstanceId);
        writer.WriteShort(Level);
        writer.WriteShort(Channel);
        writer.WriteInt((int)JobCode);
        writer.Write<Job>(Job);
        writer.WriteInt(); // CurrentHp
        writer.WriteInt(); // MaxHp
        writer.WriteShort();
        writer.WriteLong();
        writer.WriteLong(StorageCooldown);
        writer.WriteLong(DoctorCooldown);
        writer.WriteInt(ReturnMapId);
        writer.Write<Vector3>(ReturnPosition);
        writer.WriteInt(); // GearScore
        writer.Write<SkinColor>(SkinColor);
        writer.WriteLong(CreationTime);
        writer.Write<Trophy>(Trophy);
        writer.WriteLong(); // GuildId
        writer.WriteUnicodeString(); // GuildName
        writer.WriteUnicodeString(Motto);
        writer.WriteUnicodeString(Picture);
        writer.WriteByte(); // Club Count
        writer.WriteByte(); // PCBang related?
        writer.Write<Mastery>(Mastery);
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
        writer.WriteInt(Account.PrestigeLevel); // PrestigeLevel
        writer.WriteLong(LastModified);
        writer.WriteInt(); // Unknown Count
        writer.WriteInt(); // Unknown Count
        writer.WriteShort(); // Survival related?
        writer.WriteLong();
    }
}
