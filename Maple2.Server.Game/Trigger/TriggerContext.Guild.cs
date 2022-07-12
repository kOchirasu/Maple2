using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void GiveGuildExp(int boxId, byte type) { }

    public void GuildVsGameEndGame() { }

    public void GuildVsGameGiveContribution(int teamId, bool isWin, string description) { }

    public void GuildVsGameGiveReward(GuildReward type, int teamId, bool isWin, string description) { }

    public void GuildVsGameLogResult(string description) { }

    public void GuildVsGameLogWonByDefault(int teamId, string description) { }

    public void GuildVsGameResult(string description) { }

    public void GuildVsGameScoreByUser(int boxId, bool score, string description) { }

    public void SetUserValueFromGuildVsGameScore(int teamId, string key) { }

    public void SetUserValueFromUserCount(int boxId, string key, int userTagId) { }

    public void UserValueToNumberMesh(string key, int startMeshId, int digitCount) { }

    #region Conditions
    public bool GuildVsGameWinnerTeam(int teamId) {
        return false;
    }
    #endregion
}
