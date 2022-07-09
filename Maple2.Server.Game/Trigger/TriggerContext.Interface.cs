using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void CreateWidget(WidgetType type) { }

    public void GuideEvent(int eventId) { }

    public void HideGuideSummary(int entityId, int textId) { }

    public void Notice(bool arg1, string arg2, bool arg3) { }

    public void PlaySystemSoundByUserTag(int userTagId, string soundKey) { }

    public void PlaySystemSoundInBox(int[] boxIds, string soundName) { }

    public void ScoreBoardCreate(string type, int maxScore) { }

    public void ScoreBoardRemove() { }

    public void ScoreBoardSetScore(bool score) { }

    public void SetEventUI(byte type, string script, int duration, string box) { }

    public void SetVisibleUI(string uiName, bool visible) { }

    public void ShowCountUI(string text, byte stage, byte count, byte soundType) { }

    public void ShowEventResult(EventResultType type, string text, int duration, int userTagId, int triggerBoxId, bool isOutSide) { }

    public void ShowGuideSummary(int entityId, int textId, int duration) { }

    public void ShowRoundUI(byte round, int duration) { }

    public void SideNpcTalk(int npcId, string illust, int duration, string script, string voice, SideNpcTalkType type, string usm) { }

    public void WidgetAction(WidgetType type, string name, string args, int widgetArgNum) { }

    #region Conditions
    public bool WidgetCondition(WidgetType type, string arg2, string arg3) {
        return false;
    }
    #endregion
}
