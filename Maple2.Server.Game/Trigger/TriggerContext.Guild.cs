using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void GiveGuildExp(bool boxId, byte type) { }

    public void GuildVsGameEndGame() { }

    public void GuildVsGameGiveContribution(int teamId, bool isWin, string desc) { }

    public void GuildVsGameGiveReward(GuildReward type, int teamId, bool isWin, string desc) { }

    public void GuildVsGameLogResult(string desc) { }

    public void GuildVsGameLogWonByDefault(int teamId, string desc) { }

    public void GuildVsGameResult(string desc) { }

    public void GuildVsGameScoreByUser(int triggerBoxId, bool score, string desc) { }

    public void SetUserValueFromGuildVsGameScore(int teamId, string key) { }

    public void SetUserValueFromUserCount(int triggerBoxId, string key, int userTagId) { }

    public void UserValueToNumberMesh(string key, int startMeshId, byte digitCount) { }

    #region Conditions
    public bool GuildVsGameScoredTeam(int teamId) {
        return false;
    }

    public bool GuildVsGameWinnerTeam(int teamId) {
        return false;
    }
    #endregion
}
