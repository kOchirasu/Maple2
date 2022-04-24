using System;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class Character : IByteSerializable {
    #region Immutable
    public DateTime CreationTime { get; init; }
    public DateTime LastModified { get; init; }

    public long Id { get; init; }
    public long AccountId { get; init; }
    #endregion

    public DateTimeOffset DeleteTime;

    public string Name;
    public Gender Gender;
    public int MapId;
    public short Level;
    public JobCode JobCode => (JobCode)((int)Job / 10);
    public Job Job;
    
    public SkinColor SkinColor;
    public long Experience;
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
    }

    public void ReadFrom(IByteReader reader) {
        throw new NotImplementedException();
    }
}