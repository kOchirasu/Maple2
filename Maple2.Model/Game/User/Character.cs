using System;
using System.Numerics;
using Maple2.Model.Common;
using Maple2.Model.Enum;

namespace Maple2.Model.Game;

public class Character {
    #region Immutable
    public long CreationTime { get; init; }
    public long LastModified { get; init; }

    public long Id { get; init; }
    public long AccountId { get; init; }
    #endregion

    public long DeleteTime;

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
    
    public long StorageCooldown;
    public long DoctorCooldown;

    public int ReturnMapId;
    public Vector3 ReturnPosition;
    public string Motto;
    public string Picture;
    public Mastery Mastery;
}
