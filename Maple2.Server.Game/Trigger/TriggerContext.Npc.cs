using System.Numerics;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void AddBalloonTalk(int spawnPointId, string msg, int duration, int delayTick, bool npcId) { }

    public void ChangeMonster(int removeSpawnId, int addSpawnId) { }

    public void CreateMonster(int[] spawnIds, bool spawnAnimation, int arg3) { }

    public void DestroyMonster(int[] spawnIds, bool arg2) { }

    public void InitNpcRotation(int[] arg1) { }

    public void LimitSpawnNpcCount(byte limitCount) { }

    public void MoveNpc(int spawnPointId, string patrolName) { }

    public void MoveNpcToPos(int spawnPointId, Vector3 pos, Vector3 rot) { }

    public void NpcRemoveAdditionalEffect(int spawnPointId, int additionalEffectId) { }

    public void NpcToPatrolInBox(int boxId, int npcId, string spawnId, string patrolName) { }

    public void RemoveBalloonTalk(int spawnPointId) { }

    public void SetAiExtraData(string key, int value, bool isModify, int boxId) { }

    public void SetConversation(byte type, int id, string script, int delay, byte arg5, Align align) { }

    public void SetNpcDuelHpBar(bool isOpen, int spawnPointId, int durationTick, byte npcHpStep) { }

    public void SetNpcEmotionLoop(int spawnId, string sequenceName, float duration) { }

    public void SetNpcEmotionSequence(int spawnId, string sequenceName, int arg3) { }

    public void SetNpcRotation(int spawnId, int degrees) { }

    public void SpawnNpcRange(int[] rangeId, bool isAutoTargeting, byte randomPickCount, int score) { }

    #region Conditions
    public bool CheckNpcAdditionalEffect(int spawnPointId, int additionalEffectId, byte level) {
        return false;
    }

    public bool MonsterDead(int[] spawnIds, bool arg2) {
        return false;
    }

    public bool MonsterInCombat(int[] spawnIds) {
        return false;
    }

    public bool NpcDetected(int boxId, int[] spawnIds) {
        return false;
    }

    public bool NpcIsDeadByStringId(string stringId) {
        return false;
    }
    #endregion
}
