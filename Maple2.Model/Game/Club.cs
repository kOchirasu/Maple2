using System;
using System.Collections.Generic;

namespace Maple2.Model.Game;

public class Club {
    public DateTime LastModified { get; init; }

    public long Id { get; init; }
    public required string Name;
    public ClubMember Leader;
    public long CreationTime;

    public IList<ClubMember> Members;
}

public record ClubMember(
    IPlayerInfo Info,
    long JoinTime,
    long LastLoginTime);
