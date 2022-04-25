using System;

namespace Maple2.Model.Game;

public class Account {
    #region Immutable
    public long Id { get; init; }
    
    public string Username { get; init; }
    #endregion

    public long Merets { get; set; }
    public int MaxCharacters { get; set; }
    public int PrestigeLevel { get; set; }
    public long PrestigeExp { get; set; }
    public Trophy Trophy { get; set; }
    public long PremiumTime { get; set; }
}