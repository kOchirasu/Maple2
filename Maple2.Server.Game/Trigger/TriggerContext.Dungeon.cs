namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void DungeonClear(string uiType) { }

    public void DungeonClearRound(byte round) { }

    public void DungeonCloseTimer() { }

    public void DungeonDisableRanking() { }

    public void DungeonEnableGiveUp(bool isEnable) { }

    public void DungeonFail() { }

    public void DungeonMissionComplete(int missionId, string feature) { }

    public void DungeonMoveLapTimeToNow(byte id) { }

    public void DungeonResetTime(int seconds) { }

    public void DungeonSetEndTime() { }

    public void DungeonSetLapTime(byte id, int lapTime) { }

    public void DungeonStopTimer() { }

    public void RandomAdditionalEffect(string target, int boxId, int spawnId, byte targetCount, int tick, int waitTick, string targetEffect, int additionalEffectId) { }

    public void SetDungeonVariable(int varId, bool value) { }

    public void SetUserValueFromDungeonRewardCount(string key, int dungeonRewardId) { }

    public void StartTutorial() { }

    #region DarkStream
    public void DarkStreamSpawnMonster(int[] spawnIds, int score) { }

    public void DarkStreamStartGame(byte round) { }

    public void DarkStreamStartRound(byte round, int uiDuration, int damagePenalty) { }

    public void DarkStreamClearRound(byte round) { }
    #endregion

    #region ShadowExpedition
    public void ShadowExpeditionOpenBossGauge(int maxGaugePoint, string title) { }

    public void ShadowExpeditionCloseBossGauge() { }
    #endregion

    #region Conditions
    public bool CheckDungeonLobbyUserCount() {
        return false;
    }

    public bool DungeonTimeOut() {
        return false;
    }

    public bool IsDungeonRoom() {
        return false;
    }

    public bool IsPlayingMapleSurvival() {
        return false;
    }
    #endregion
}
