using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using Serilog;

namespace Maple2.Server.Game.Manager;

public sealed class TrophyManager : IDisposable {
    private const int BATCH_SIZE = 60;
    private readonly GameSession session;
    
    public IDictionary<int, TrophyEntry> Values { get; }

    public TrophyManager(GameSession session) {
        this.session = session;
        
        using GameStorage.Request db = session.GameStorage.Context();
        Values = db.GetAccountTrophy(session.AccountId);
    }

    public void Dispose() {
        //using GameStorage.Request db = session.GameStorage.Context();
        //db.SaveItems(session.CharacterId, items.ToArray());
    }

    public void Load() {
        session.Send(TrophyPacket.Initialize());
        foreach (ImmutableList<TrophyEntry> batch in Values.Values.Batch(BATCH_SIZE)) {
            session.Send(TrophyPacket.Load(batch));
        }
    }

    public void Update(TrophyConditionType conditionType, long count = 1) {
        IEnumerable<TrophyMetadata> metadatas = session.TrophyMetadata.GetMany(conditionType);

        foreach (TrophyMetadata metadata in metadatas) {
            if (!Values.TryGetValue(metadata.Id, out TrophyEntry? trophy) || !metadata.Grades.TryGetValue(trophy.CurrentGrade, out TrophyMetadataGrade? grade)) {
                grade = metadata.Grades[1];
            }

            Console.WriteLine(metadata.Id);
            if (!CheckCondition(conditionType, grade.Condition)) {
                continue;
            }

            if (trophy == null) {
                trophy = new TrophyEntry(metadata) {
                    CurrentGrade = 1,
                    RewardGradeReceived = 1,
                };
            }
            
            RankUp(trophy, count);
            session.Send(TrophyPacket.Update(trophy));
        }
    }

    
    public bool CheckCondition(TrophyConditionType conditionType, TrophyMetadataCondition condition) {
        // check code
        switch (conditionType) {
            case TrophyConditionType.map:
                int[] mapIds = condition.Code.Select(int.Parse).ToArray();
                if (mapIds.Contains(session.Player.Value.Character.MapId)) {
                    return true;
                }
                break;
        }
        return false;
    }

    /// <summary>
    /// Checks if trophy has reached a new grade. Provides rewards only on certain reward types.
    /// </summary>
    /// <param name="trophy"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public bool RankUp(TrophyEntry trophy, long count) {
        trophy.Counter += count;
        if (!trophy.Metadata.Grades.TryGetValue(trophy.CurrentGrade, out TrophyMetadataGrade? grade)) {
            // Next grade does not exist.
            return false;
        }
        if (trophy.Counter < grade.Condition.Value) {
            return false;
        }

        trophy.GradesReceived.Add(trophy.CurrentGrade, DateTime.Now.ToEpochSeconds());
        trophy.CurrentGrade++;
        return true;
    }
}
