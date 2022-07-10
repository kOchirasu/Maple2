using System.Numerics;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void EnableSpawnPointPc(int spawnPointId, bool isEnable) { }

    public void GiveRewardContent(int rewardId) { }

    public void KickMusicAudience(int targetBoxId, int targetPortalId) { }

    public void MoveRandomUser(int mapId, int portalId, int triggerId, int count) { }

    public void MoveToPortal(int userTagId, int portalId, int boxId) { }

    public void MoveUser(int mapId, int portalId, int boxId) { }

    public void MoveUserPath(string path) { }

    public void MoveUserToBox(int boxId, int portalId) { }

    public void MoveUserToPos(Vector3 position, Vector3 rotation) { }

    public void PatrolConditionUser(string patrolName, byte patrolIndex, int additionalEffectId) { }

    public void SetAchievement(string type, string code, int triggerId) { }

    public void SetPcEmotionLoop(string sequenceName, float duration, bool arg3) { }

    public void SetPcEmotionSequence(string[] sequenceNames) { }

    public void SetPcRotation(Vector3 rotation) { }

    public void SetQuestAccept(int questId) { }

    public void SetQuestComplete(int questId) { }

    public void TalkNpc(int spawnId) { }

    public void AddUserValue(string key, int value) { }

    public void SetUserValue(string key, int value, int triggerId) { }

    public void FaceEmotion(int spawnId, string emotionName) { }

    public void SetState(byte arg1, string[] arg2, bool arg3) { }

    #region Conditions
    public bool CheckAnyUserAdditionalEffect(int boxId, int additionalEffectId, short level) {
        return false;
    }

    public bool CheckSameUserTag(int boxId) {
        return false;
    }

    public bool QuestUserDetected(int[] boxIds, int[] questIds, byte[] questStates, byte jobCode) {
        return false;
    }

    public bool UserDetected(int[] boxIds, byte jobCode) {
        return false;
    }
    #endregion
}
