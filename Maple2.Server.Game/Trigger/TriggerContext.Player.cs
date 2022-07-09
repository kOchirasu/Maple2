using System.Numerics;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void EnableSpawnPointPc(int spawnPointId, bool isEnable) { }

    public void GiveExp(byte arg1, byte arg2) { }

    public void GiveRewardContent(int rewardId) { }

    public void KickMusicAudience(int targetBoxId, int targetPortalId) { }

    public void MoveRandomUser(int arg1, byte arg2, int arg3, byte arg4) { }

    public void MoveToPortal(int userTagId, int portalId, int boxId) { }

    public void MoveUser(int mapId, int portalId, int boxId) { }

    public void MoveUserPath(string path) { }

    public void MoveUserToBox(int boxId, bool portalId) { }

    public void MoveUserToPos(Vector3 pos, Vector3 rot) { }

    public void PatrolConditionUser(string patrolName, byte patrolIndex, int additionalEffectId) { }

    public void SetAchievement(int target, string type, string code) { }

    public void SetPcEmotionLoop(string arg1, float arg2, bool arg3) { }

    public void SetPcEmotionSequence(string arg1) { }

    public void SetPcRotation(Vector3 rotation) { }

    public void SetQuestAccept(int questId, int arg1) { }

    public void SetQuestComplete(int questId) { }

    public void TalkNpc(int spawnPointId) { }

    public void AddUserValue(string key, int value) { }

    public void SetUserValue(int triggerId, string key, int value) { }

    public void FaceEmotion(int spawnPointId, string emotionName) { }

    public void SetState(byte arg1, string arg2, bool arg3) { }

    #region Conditions
    public bool CheckAnyUserAdditionalEffect(int triggerBoxId, int additionalEffectId, byte level) {
        return false;
    }

    public bool CheckSameUserTag(int triggerBoxId) {
        return false;
    }

    public bool QuestUserDetected(int[] arg1, int[] arg2, byte[] arg3, byte arg4) {
        return false;
    }

    public bool UserDetected(int[] boxIds, byte jobCode) {
        return false;
    }
    #endregion
}
