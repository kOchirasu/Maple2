using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

public class Quest {
    public int Id { get; set; }
    public long OwnerId { get; set; }
    public QuestState State { get; set; }
    public int CompletionCount { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public bool Track { get; set; }
    public SortedDictionary<int, QuestCondition> Conditions { get; set; }

    public Quest() {
        Conditions = new SortedDictionary<int, QuestCondition>();
    }

    [return:NotNullIfNotNull(nameof(other))]
    public static implicit operator Quest?(Maple2.Model.Game.Quest? other) {
        if (other == null) {
            return null;
        }
        var quest = new Quest {
            Id = other.Id,
            State = other.State,
            CompletionCount = other.CompletionCount,
            StartTime = other.StartTime,
            EndTime = other.EndTime,
            Track = other.Track,
        };
        
        foreach ((int index, QuestCondition condition) in other.Conditions) {
            quest.Conditions.Add(index, condition);
        }

        return quest;
    }

    // Use explicit Convert() here because we need metadata to construct Quest.
    public Maple2.Model.Game.Quest Convert(QuestMetadata metadata) {
        var quest = new Maple2.Model.Game.Quest(metadata) {
            State = State,
            CompletionCount = CompletionCount,
            StartTime = StartTime,
            EndTime = EndTime,
            Track = Track,
        };
        
        for (int i = 0; i < Conditions.Count; i++) {
            quest.Conditions.Add(i, Conditions[i].Convert(metadata.Conditions[i]));
        }

        return quest;
    }
    
    public static void Configure(EntityTypeBuilder<Quest> builder) {
        builder.HasKey(quest => new {quest.OwnerId, quest.Id});
        builder.Property(quest => quest.Conditions).HasJsonConversion().IsRequired();
    }
}

public class QuestCondition {
    public int Counter { get; set; }
    
    [return:NotNullIfNotNull(nameof(other))]
    public static implicit operator QuestCondition?(Maple2.Model.Game.QuestCondition? other) {
        return other == null ? null : new QuestCondition {
            Counter = other.Counter,
        };
    }
    
    // Use explicit Convert() here because we need metadata to construct Quest.
    public Maple2.Model.Game.QuestCondition Convert(QuestMetadataCondition metadata) {
        return new Maple2.Model.Game.QuestCondition(metadata) {
            Counter = Counter,
        };
    }
}
