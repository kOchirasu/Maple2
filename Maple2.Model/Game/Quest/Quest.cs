using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class Quest : IByteSerializable {
    public readonly int Id;

    public QuestState State;
    public int CompletionCount;
    public long StartTime;
    public long EndTime;
    public long ExpiryTime;
    public bool Accepted;
    public IList<int> Conditions;

    public Quest(int id) {
        Id = id;
        Conditions = new List<int>();
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.Write<QuestState>(State);
        writer.WriteInt(CompletionCount);
        writer.WriteLong(StartTime);
        writer.WriteLong(EndTime);
        writer.WriteBool(Accepted);

        writer.WriteInt(Conditions.Count);
        foreach (int condition in Conditions) {
            writer.WriteInt(condition);
        }
    }
}
