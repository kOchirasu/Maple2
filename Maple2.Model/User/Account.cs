using System;

namespace Maple2.Model.User;

public class Account {
    #region Immutable
    public DateTime LastModified { get; init; }

    public long Id { get; init; }
    #endregion

    public long Merets { get; set; }
    public int MaxCharacters { get; set; }
    public int PrestigeLevel { get; set; }
    public long PrestigeExp { get; set; }
    public long PremiumTime { get; set; }
}