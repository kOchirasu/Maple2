using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class Quest : IByteSerializable {
    public readonly int Id;
    public readonly QuestMetadata Metadata;

    public QuestState State;
    public int CompletionCount;
    public long StartTime;
    public long EndTime;
    public bool Track;
    public SortedDictionary<int, QuestCondition> Conditions;

    public Quest(QuestMetadata metadata) {
        Id = metadata.Id;
        Metadata = metadata;
        Conditions = new SortedDictionary<int, QuestCondition>();
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.Write<QuestState>(State);
        writer.WriteInt(CompletionCount);
        writer.WriteLong(StartTime);
        writer.WriteLong(EndTime);
        writer.WriteBool(Track);

        writer.WriteInt(Conditions.Count);
        foreach (QuestCondition condition in Conditions.Values) {
            writer.WriteInt(condition.Counter);
        }
    }
}

public class QuestCondition {
    public readonly QuestMetadataCondition Metadata;
    public int Counter;

    public QuestCondition(QuestMetadataCondition metadata) {
        Metadata = metadata;
    }
}
