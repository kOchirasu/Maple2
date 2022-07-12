using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void EndMiniGame(int winnerBoxId, MiniGame type, bool isOnlyWinner) { }

    public void EndMiniGameRound(int winnerBoxId, float expRate, bool isOnlyWinner, bool isGainLoserBonus, bool meso, MiniGame type) { }

    public void MiniGameCameraDirection(int boxId, int cameraId) { }

    public void MiniGameGiveExp(int boxId, float expRate, bool isOutSide) { }

    public void MiniGameGiveReward(int winnerBoxId, string contentType, MiniGame type) { }

    public void SetMiniGameAreaForHack(int boxId) { }

    public void StartMiniGame(int boxId, byte round, MiniGame type, bool isShowResultUi) { }

    public void StartMiniGameRound(int boxId, byte round) { }

    public void UnSetMiniGameAreaForHack() { }

    public void UseState(byte arg1, bool arg2) { }

    #region CathyMart
    public void AddEffectNif(int spawnPointId, string nifPath, bool isOutline, float scale, int rotateZ) { }

    public void RemoveEffectNif(int spawnPointId) { }
    #endregion

    #region HideAndSeek
    public void CreateFieldGame(FieldGame type, bool reset) { }

    public void FieldGameConstant(string key, string value, string feature, Locale locale) { }

    public void FieldGameMessage(byte custom, string type, byte arg1, string script, int duration) { }
    #endregion

    #region Conditions
    public bool BonusGameRewardDetected(int boxId) {
        return false;
    }
    #endregion
}
