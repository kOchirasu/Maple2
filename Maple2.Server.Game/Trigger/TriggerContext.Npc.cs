using System.Linq;
using System.Numerics;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void ChangeMonster(int removeSpawnId, int addSpawnId) {
        DebugLog("[ChangeMonster] removeSpawnId:{RemoveId}, addSpawnId:{AddId}", removeSpawnId, addSpawnId);
        foreach (FieldNpc fieldMob in Field.Mobs.Values) {
            if (fieldMob.SpawnPointId == removeSpawnId) {
                Field.RemoveNpc(fieldMob.ObjectId);
            }
        }

        SpawnMonster(addSpawnId);
    }

    public void CreateMonster(int[] spawnIds, bool spawnAnimation, int arg3) {
        WarnLog("[CreateMonster] spawnIds:{SpawnIds}, spawnAnimation:{SpawnAnimation}, arg3:{Arg3}", string.Join(", ", spawnIds), spawnAnimation, arg3);
        foreach (int spawnId in spawnIds) {
            SpawnMonster(spawnId);
        }
    }

    public void DestroyMonster(int[] spawnIds, bool arg2) {
        WarnLog("[DestroyMonster] spawnIds:{SpawnIds}, Arg2:{Arg2}", string.Join(", ", spawnIds), arg2);
        if (spawnIds.Contains(-1)) {
            foreach (int objectId in Field.Mobs.Keys) {
                Field.RemoveNpc(objectId);
            }
            return;
        }

        foreach (FieldNpc fieldMob in Field.Mobs.Values) {
            if (spawnIds.Contains(fieldMob.SpawnPointId)) {
                Field.RemoveNpc(fieldMob.ObjectId);
            }
        }
    }

    private void SpawnMonster(int spawnId) {
        if (!Field.Entities.EventNpcSpawns.TryGetValue(spawnId, out EventSpawnPointNPC? spawn)) {
            logger.Error("[SpawnMonster] Invalid spawnId:{SpawnId}", spawnId);
            return;
        }

        foreach (int npcId in spawn.NpcIds) {
            if (!Field.NpcMetadata.TryGet(npcId, out NpcMetadata? npc)) {
                logger.Error("[SpawnMonster] Invalid npcId:{NpcId}", npcId);
                continue;
            }

            FieldNpc fieldNpc = Field.SpawnNpc(npc, spawn.Position, spawn.Rotation);
            fieldNpc.SpawnPointId = spawnId;

            Field.Broadcast(FieldPacket.AddNpc(fieldNpc));
            Field.Broadcast(ProxyObjectPacket.AddNpc(fieldNpc));
        }
    }

    public void InitNpcRotation(int[] spawnIds) {
        ErrorLog("[InitNpcRotation] spawnIds:{SpawnIds}", string.Join(", ", spawnIds));
    }

    public void LimitSpawnNpcCount(int limitCount) {
        ErrorLog("[LimitSpawnNpcCount] limitCount:{Count}", limitCount);
    }

    public void MoveNpc(int spawnId, string patrolName) {
        ErrorLog("[MoveNpc] spawnId:{SpawnId}", spawnId);
    }

    public void MoveNpcToPos(int spawnId, Vector3 position, Vector3 rotation) {
        ErrorLog("[MoveNpc] spawnId:{SpawnId}", spawnId);
    }

    public void NpcRemoveAdditionalEffect(int spawnId, int additionalEffectId) {
        ErrorLog("[MoveNpc] spawnId:{SpawnId}", spawnId);
    }

    public void NpcToPatrolInBox(int boxId, int npcId, string spawnId, string patrolName) {
        ErrorLog("[NpcToPatrolInBox] boxId:{BoxId}, npcId:{NpcId}, spawnId:{SpawnId}, patrolName:{PatrolName}", spawnId, npcId, spawnId, patrolName);
    }

    public void SetAiExtraData(string key, int value, bool isModify, int boxId) {
        ErrorLog("[SetAiExtraData] key:{Key}, value:{Value}, isModify:{IsModify}, boxId:{BoxId}", key, value, isModify, boxId);
    }

    public void SetConversation(byte type, int spawnId, string script, int delay, byte arg5, Align align) {
        ErrorLog("[SetConversation] type:{Type}, spawnId:{SpawnId}, script:{Script}, delay:{Delay}, arg5:{Arg5}, align:{Align}",
            type, spawnId, script, delay, arg5, align);
    }

    public void SetNpcDuelHpBar(bool isOpen, int spawnId, int durationTick, int npcHpStep) {
        ErrorLog("[SetNpcDuelHpBar] isOpen:{IsOpen}, spawnId:{SpawnId}, durationTick:{Duration}, npcHpStep:{HpStep}", isOpen, spawnId, durationTick, npcHpStep);
    }

    public void SetNpcEmotionLoop(int spawnId, string sequenceName, float duration) {
        ErrorLog("[SetNpcEmotionLoop] spawnId:{SpawnId}, sequenceName:{SequenceName}, durationTick:{Duration}", spawnId, sequenceName, duration);
    }

    public void SetNpcEmotionSequence(int spawnId, string sequenceName, int durationTick) {
        ErrorLog("[SetNpcEmotionSequence] spawnId:{SpawnId}, sequenceName:{SequenceName}, durationTick:{Duration}", spawnId, sequenceName, durationTick);
    }

    public void SetNpcRotation(int spawnId, float rotation) {
        ErrorLog("[SetNpcRotation] spawnId:{SpawnId}, rotation:{Rotation}", spawnId, rotation);
    }

    public void SpawnNpcRange(int[] spawnIds, bool isAutoTargeting, int randomPickCount, int score) {
        ErrorLog("[SpawnNpcRange] spawnIds:{SpawnIds}, isAutoTargeting:{AutoTarget}, randomPickCount:{RandomCount}, score:{Score}",
            string.Join(", ", spawnIds), isAutoTargeting, randomPickCount, score);
    }

    #region Conditions
    public bool CheckNpcAdditionalEffect(int spawnId, int additionalEffectId, short level) {
        ErrorLog("[CheckNpcAdditionalEffect] spawnId:{SpawnId}, additionalEffectId:{EffectId}, level:{Level}", spawnId, additionalEffectId, level);
        return false;
    }

    public bool MonsterDead(int[] spawnIds, bool arg2) {
        ErrorLog("[MonsterDead] spawnIds:{SpawnIds}, arg2:{Arg2}", string.Join(", ", spawnIds), arg2);
        return false;
    }

    public bool MonsterInCombat(int[] spawnIds) {
        ErrorLog("[MonsterInCombat] spawnIds:{SpawnIds}", string.Join(", ", spawnIds));
        return false;
    }

    public bool NpcDetected(int boxId, int[] spawnIds) {
        ErrorLog("[NpcDetected] boxId:{BoxId}, spawnIds:{SpawnIds}", boxId, string.Join(", ", spawnIds));
        return false;
    }

    public bool NpcIsDeadByStringId(string stringId) {
        ErrorLog("[NpcIsDeadByStringId] stringId:{Round}", stringId);
        return false;
    }
    #endregion
}
