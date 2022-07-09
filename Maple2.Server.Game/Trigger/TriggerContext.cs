using Maple2.Trigger;
using Serilog;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext : ITriggerContext {
    private readonly ILogger logger = Log.Logger.ForContext<TriggerContext>();

    // Accessors
    public int GetShadowExpeditionPoints() {
        return 0;
    }

    public bool GetDungeonVariable(int id) {
        return false;
    }

    public float GetNpcDamageRate(int spawnPointId) {
        return 1.0f;
    }

    public float GetNpcHpRate(int spawnPointId) {
        return 1.0f;
    }

    public int GetDungeonId() {
        return 0;
    }

    public int GetDungeonLevel() {
        return 3;
    }

    public int GetDungeonMaxUserCount() {
        return 1;
    }

    public int GetDungeonRoundsRequired() {
        return int.MaxValue;
    }

    public int GetUserCount(int boxId, int userTagId) {
        return 1;
    }

    public int GetNpcExtraData(int spawnPointId, string extraDataKey) {
        return 0;
    }

    public int GetDungeonPlayTime() {
        return 0;
    }

    // Scripts seem to just check if this is "Fail"
    public string GetDungeonState() {
        return "";
    }

    public int GetDungeonFirstUserMissionScore() {
        return 0;
    }

    public int GetScoreBoardScore() {
        return 0;
    }

    public int GetUserValue(string key) {
        return 0;
    }

    public void DebugString(string value, string feature) {
        logger.Debug("{Value} [{Feature}]", value, feature);
    }

    public void WriteLog(string log, int arg2, string arg3, byte arg4, string arg5) {
        logger.Information("{Log}: {Arg2}, {Arg3}, {Arg4}, {Arg5}", log, arg2, arg3, arg4, arg5);
    }

    #region Conditions
    public bool DayOfWeek(byte[] dayOfWeeks, string desc) {
        return false;
    }

    public bool RandomCondition(float arg1, string desc) {
        return true;
    }

    public bool WaitAndResetTick(int waitTick) {
        return true;
    }

    public bool WaitTick(int waitTick) {
        return true;
    }
    #endregion
}
