using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class AchievementInfo : IByteSerializable {
    public int Combat { get; set; }
    public int Adventure { get; set; }
    public int Lifestyle { get; set; }

    public static AchievementInfo operator +(in AchievementInfo a, in AchievementInfo b) {
        return new AchievementInfo {
            Combat = a.Combat + b.Combat,
            Adventure = a.Adventure + b.Adventure,
            Lifestyle = a.Lifestyle + b.Lifestyle,
        };
    }

    public static AchievementInfo operator -(in AchievementInfo a, in AchievementInfo b) {
        return new AchievementInfo {
            Combat = a.Combat - b.Combat,
            Adventure = a.Adventure - b.Adventure,
            Lifestyle = a.Lifestyle - b.Lifestyle,
        };
    }
    public int Total => Combat + Adventure + Lifestyle;
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Combat);
        writer.WriteInt(Adventure);
        writer.WriteInt(Lifestyle);
    }
}

public class Achievement : IByteSerializable {
    public readonly AchievementMetadata Metadata;
    public readonly int Id;
    public bool Completed => CurrentGrade == Grades.Count;
    public AchievementStatus Status => Completed ? AchievementStatus.Completed : AchievementStatus.InProgress;
    public int CurrentGrade;
    public int RewardGrade;
    public bool Favorite;
    public long Counter;
    public AchievementCategory Category { get; init; }

    public IDictionary<int, long> Grades { get; set; } = new Dictionary<int, long>();

    public Achievement(AchievementMetadata metadata) {
        Metadata = metadata;
        Category = metadata.Category;
        Id = metadata.Id;
    }

    public void WriteTo(IByteWriter writer) {
        writer.Write<AchievementStatus>(Status);
        writer.WriteInt(Completed ? 1 : 0);
        writer.WriteInt(CurrentGrade);
        writer.WriteInt(RewardGrade);
        writer.WriteBool(Favorite);
        writer.WriteLong(Counter);
        writer.WriteInt(Grades.Count);

        foreach ((int grade, long timeAcquired) in Grades.OrderBy(grade => grade.Key).ToList()) {
            writer.WriteInt(grade);
            writer.WriteLong(timeAcquired);
        }
    }
}
