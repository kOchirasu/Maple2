using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
public struct Trophy {
    public int Combat { get; set; }
    public int Adventure { get; set; }
    public int Lifestyle { get; set; }

    public static Trophy operator +(in Trophy a, in Trophy b) {
        return new Trophy {
            Combat = a.Combat + b.Combat,
            Adventure = a.Adventure + b.Adventure,
            Lifestyle = a.Lifestyle + b.Lifestyle,
        };
    }

    public static Trophy operator -(in Trophy a, in Trophy b) {
        return new Trophy {
            Combat = a.Combat - b.Combat,
            Adventure = a.Adventure - b.Adventure,
            Lifestyle = a.Lifestyle - b.Lifestyle,
        };
    }
    public int Total => Combat + Adventure + Lifestyle;
}

public class TrophyEntry : IByteSerializable {
    public readonly TrophyMetadata Metadata;
    public int Id;
    public bool Completed => CurrentGrade == Metadata.Grades.Count;
    public TrophyStatus Status => Completed ? TrophyStatus.Completed : TrophyStatus.InProgress;
    public int CurrentGrade;
    public int RewardGradeReceived;
    public bool Favorite;
    public long Counter;
    public TrophyCategory Category { get; init; }

    public IDictionary<int, long> GradesReceived { get; set; } = new Dictionary<int, long>();

    public TrophyEntry(TrophyMetadata metadata) {
        Metadata = metadata;
        Category = metadata.Category;
        Id = metadata.Id;
    }

    public void RankUp() {
        if (!Metadata.Grades.TryGetValue(CurrentGrade + 1, out TrophyMetadataGrade? gradeMetadata)) {
            return;
        }

        CurrentGrade++;
        
    }

    public void WriteTo(IByteWriter writer) {
        writer.Write<TrophyStatus>(Status);
        writer.WriteInt(Completed ? 1 : 0);
        writer.WriteInt(CurrentGrade);
        writer.WriteInt(RewardGradeReceived);
        writer.WriteBool(Favorite);
        writer.WriteLong(Counter);
        writer.WriteInt(GradesReceived.Count);

        foreach ((int grade, long timeAcquired) in GradesReceived.OrderBy(grade => grade.Key).ToList()) {
            writer.WriteInt(grade);
            writer.WriteLong(timeAcquired);
        }    
    }
}
