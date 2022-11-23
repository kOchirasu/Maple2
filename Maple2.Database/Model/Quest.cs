using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Maple2.Model.Enum;

namespace Maple2.Database.Model;

public class Quest {
    public int Id { get; set; }
    public QuestState State { get; set; }
    public int CompletionCount { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public long ExpiryTime { get; set; }
    public bool Accepted { get; set; }
    public IList<int> Conditions { get; set; }

    public Quest() {
        Conditions = new List<int>();
    }

    [return:NotNullIfNotNull(nameof(other))]
    public static implicit operator Quest?(Maple2.Model.Game.Quest? other) {
        return other == null ? null : new Quest {
            Id = other.Id,
            State = other.State,
            CompletionCount = other.CompletionCount,
            StartTime = other.StartTime,
            EndTime = other.EndTime,
            ExpiryTime = other.ExpiryTime,
            Accepted = other.Accepted,
            Conditions = other.Conditions,
        };
    }

    [return:NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Quest?(Quest? other) {
        return other == null ? null : new Maple2.Model.Game.Quest(other.Id) {
            State = other.State,
            CompletionCount = other.CompletionCount,
            StartTime = other.StartTime,
            EndTime = other.EndTime,
            ExpiryTime = other.ExpiryTime,
            Accepted = other.Accepted,
            Conditions = other.Conditions,
        };
    }
}
