using System;
using System.Collections.Generic;

namespace Maple2.Model.Game;

public class Club {
    public DateTime LastModified { get; init; }

    public long Id { get; init; }
    public string Name;
    public ClubMember Leader;
    public long CreationTime;

    public IList<ClubMember> Members;
}

public record ClubMember(
    CharacterInfo Info,
    long JoinTime,
    long LastLoginTime,
    bool Online);
