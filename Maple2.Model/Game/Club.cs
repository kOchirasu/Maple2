using System.Collections.Generic;

namespace Maple2.Model.Game; 

public class Club {
    public long Id { get; init; }
    public string Name;
    public ClubMember Leader;
    public long CreationTime;
    public long LastModified;

    public IList<ClubMember> Members;
}

public record ClubMember(
    CharacterInfo Info,
    long JoinTime,
    long LastLoginTime,
    bool Online);
