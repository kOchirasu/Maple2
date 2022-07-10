using System.Numerics;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void AddBalloonTalk(int spawnId, string msg, int duration, int delayTick) { }

    public void ChangeMonster(int removeSpawnId, int addSpawnId) { }

    public void CreateMonster(int[] spawnIds, bool spawnAnimation, int arg3) { }

    public void DestroyMonster(int[] spawnIds, bool arg2) { }

    public void InitNpcRotation(int[] spawnIds) { }

    public void LimitSpawnNpcCount(int limitCount) { }

    public void MoveNpc(int spawnId, string patrolName) { }

    public void MoveNpcToPos(int spawnId, Vector3 position, Vector3 rotation) { }

    public void NpcRemoveAdditionalEffect(int spawnId, int additionalEffectId) { }

    public void NpcToPatrolInBox(int boxId, int npcId, string spawnId, string patrolName) { }

    public void RemoveBalloonTalk(int spawnId) { }

    public void SetAiExtraData(string key, int value, bool isModify, int boxId) { }

    public void SetConversation(byte type, int spawnId, string script, int delay, byte arg5, Align align) { }

    public void SetNpcDuelHpBar(bool isOpen, int spawnPointId, int durationTick, byte npcHpStep) { }

    public void SetNpcEmotionLoop(int spawnId, string sequenceName, float duration) { }

    public void SetNpcEmotionSequence(int spawnId, string sequenceName, int durationTick) { }

    public void SetNpcRotation(int spawnId, float rotation) { }

    public void SpawnNpcRange(int[] spawnIds, bool isAutoTargeting, byte randomPickCount, int score) { }

    #region Conditions
    public bool CheckNpcAdditionalEffect(int spawnId, int additionalEffectId, short level) {
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
