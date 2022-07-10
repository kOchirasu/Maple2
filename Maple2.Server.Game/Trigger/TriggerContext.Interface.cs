using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void CreateWidget(WidgetType type) { }

    public void GuideEvent(int eventId) { }

    public void HideGuideSummary(int entityId, int textId) { }

    public void Notice(bool arg1, string script, bool arg3) { }

    public void PlaySystemSoundByUserTag(int userTagId, string soundKey) { }

    public void PlaySystemSoundInBox(string sound, int[]? boxIds) { }

    public void ScoreBoardCreate(string type, int maxScore) { }

    public void ScoreBoardRemove() { }

    public void ScoreBoardSetScore(bool score) { }

    public void SetEventUI(byte type, string script, int duration, int boxId, int notBoxId) { }

    public void SetVisibleUI(string[] uiNames, bool visible) { }

    public void ShowCountUI(string text, byte stage, byte count, byte soundType) { }

    public void ShowEventResult(EventResultType type, string text, int duration, int userTagId, int triggerBoxId, bool isOutSide) { }

    public void ShowGuideSummary(int entityId, int textId, int duration) { }

    public void ShowRoundUI(byte round, int duration) { }

    public void SideNpcTalk(int npcId, string illust, int duration, string script, string voice, SideNpcTalkType type, string usm) { }

    public void WidgetAction(WidgetType type, string name, int widgetArgType, string args) { }

    #region Conditions
    public bool WidgetCondition(WidgetType type, string condition, string value) {
        return false;
    }
    #endregion
}
