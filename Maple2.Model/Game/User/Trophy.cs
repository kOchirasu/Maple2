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

public class Trophy : IByteSerializable {
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
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Combat);
        writer.WriteInt(Adventure);
        writer.WriteInt(Lifestyle);
    }
}

public class TrophyEntry : IByteSerializable {
    public readonly TrophyMetadata Metadata;
    public readonly int Id;
    public bool Completed => CurrentGrade == Grades.Count;
    public TrophyStatus Status => Completed ? TrophyStatus.Completed : TrophyStatus.InProgress;
    public int CurrentGrade;
    public int RewardGrade;
    public bool Favorite;
    public long Counter;
    public TrophyCategory Category { get; init; }

    public IDictionary<int, long> Grades { get; set; } = new Dictionary<int, long>();

    public TrophyEntry(TrophyMetadata metadata) {
        Metadata = metadata;
        Category = metadata.Category;
        Id = metadata.Id;
    }

    public void WriteTo(IByteWriter writer) {
        writer.Write<TrophyStatus>(Status);
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
