using System;
using Maple2.Model.Common;
using Maple2.Model.Enum;

namespace Maple2.Model.Game;

public class Character {
    #region Immutable
    public DateTime CreationTime { get; init; }
    public DateTime LastModified { get; init; }

    public long Id { get; init; }
    public long AccountId { get; init; }
    #endregion

    public DateTimeOffset DeleteTime { get; set; }
    
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public Job Job { get; set; }
    public short Level { get; set; }
    public SkinColor SkinColor { get; set; }
    public long Experience { get; set; }
    public long RestExp { get; set; }
    public int MapId { get; set; }
    public int Title { get; set; }
    public short Insignia { get; set; }
}