namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    #region BoomBoomOcean
    public void ArcadeBoomBoomOceanSetSkillScore(int skillId, int score) { }

    public void ArcadeBoomBoomOceanStartGame(int lifeCount) { }

    public void ArcadeBoomBoomOceanEndGame() { }

    public void ArcadeBoomBoomOceanStartRound(int round, int roundDuration, int timeScoreRate) { }

    public void ArcadeBoomBoomOceanClearRound(int round) { }
    #endregion

    #region SpringFarm
    public void ArcadeSpringFarmSetInteractScore(int interactId, int score) { }

    public void ArcadeSpringFarmSpawnMonster(int[] spawnIds, int score) { }

    public void ArcadeSpringFarmStartGame(int lifeCount) { }

    public void ArcadeSpringFarmEndGame() { }

    public void ArcadeSpringFarmStartRound(int round, int uiDuration, string timeScoreType, int timeScoreRate, int roundDuration) { }

    public void ArcadeSpringFarmClearRound(int round) { }
    #endregion

    #region ThreeTwoOne
    public void ArcadeThreeTwoOneStartGame(int lifeCount, int initScore) { }

    public void ArcadeThreeTwoOneEndGame() { }

    public void ArcadeThreeTwoOneStartRound(int round, int uiDuration) { }

    public void ArcadeThreeTwoOneResultRound(byte resultDirection) { }

    public void ArcadeThreeTwoOneResultRound2(int round) { }

    public void ArcadeThreeTwoOneClearRound(int round) { }
    #endregion
}
