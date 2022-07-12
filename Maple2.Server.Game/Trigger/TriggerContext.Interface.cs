using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Tools.Extensions;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void CreateWidget(WidgetType type) { }

    public void GuideEvent(int eventId) {
        Broadcast(TriggerPacket.UiGuide(eventId));
    }

    public void HideGuideSummary(int entityId, int textId) {
        Broadcast(TriggerPacket.UiHideSummary(entityId));
    }

    public void Notice(bool arg1, string script, bool arg3) {
        Broadcast(NoticePacket.Notice(NoticePacket.Flags.Mint, new InterfaceText(script), 3000));
    }

    public void PlaySystemSoundByUserTag(int userTagId, string soundKey) {
        foreach (FieldPlayer player in Field.Players.Values) {
            if (player.TagId == userTagId) {
                player.Session.Send(SoundPacket.System(soundKey));
            }
        }
    }

    public void PlaySystemSoundInBox(string sound, params int[] boxIds) {
        if (boxIds.Length == 0) {
            Broadcast(SoundPacket.System(sound));
            return;
        }

        foreach (int boxId in boxIds) {
            foreach (FieldPlayer player in PlayersInBox(boxId)) {
                player.Session.Send(SoundPacket.System(sound));
            }
        }
    }

    public void ScoreBoardCreate(string type, int maxScore) { }

    public void ScoreBoardRemove() { }

    public void ScoreBoardSetScore(bool score) { }

    public void SetEventUI(byte type, string script, int duration, int boxId, int notBoxId) {
        Func<ByteWriter> getPacket;
        switch (type) {
            case 0:
                IList<int> list = script.Split(",")
                    .Select(str => int.TryParse(str, out int result) ? result : 0)
                    .ToList();
                int round = list.ElementAtOrDefault(0, 1);
                int maxRound = list.ElementAtOrDefault(1, 1);
                int minRound = list.ElementAtOrDefault(2, 1);

                getPacket = () => MassiveEventPacket.Round(round, maxRound, minRound);
                break;
            case 1:
                getPacket = () => MassiveEventPacket.Banner(BannerType.Text, script, duration);
                break;
            case 2: // arg3=0,3
                getPacket = () => MassiveEventPacket.Countdown(script, 0, 3);
                break;
            case 3:
                getPacket = () => MassiveEventPacket.Banner(BannerType.Winner, script, duration);
                break;
            case 4:
                getPacket = () => MassiveEventPacket.Banner(BannerType.Lose, script, duration);
                break;
            case 5:
                getPacket = () => MassiveEventPacket.Banner(BannerType.GameOver, script, duration);
                break;
            case 6:
                getPacket = () => MassiveEventPacket.Banner(BannerType.Bonus, script, duration);
                break;
            case 7:
                getPacket = () => MassiveEventPacket.Banner(BannerType.Success, script, duration);
                break;
            default:
                return;
        }

        if (notBoxId > 0) {
            foreach (FieldPlayer player in PlayersNotInBox(notBoxId)) {
                player.Session.Send(getPacket());
            }
        } else {
            foreach (FieldPlayer player in PlayersInBox(boxId)) {
                player.Session.Send(getPacket());
            }
        }
    }

    public void SetVisibleUI(string[] uiNames, bool visible) { }

    public void ShowCountUI(string text, byte stage, byte count, byte soundType) {
        Broadcast(MassiveEventPacket.Countdown(text, stage, count, soundType));
    }

    public void ShowEventResult(EventResultType type, string text, int duration, int userTagId, int triggerBoxId, bool isOutSide) { }

    public void ShowGuideSummary(int entityId, int textId, int duration) {
        Broadcast(TriggerPacket.UiShowSummary(entityId, textId, duration));
    }

    public void ShowRoundUI(int round, int duration) {
        Broadcast(MassiveEventPacket.StartRound(round, duration));
    }

    public void SideNpcTalk(int npcId, string illust, int duration, string script, string voice, SideNpcTalkType type, string usm) {
        switch (type) {
            case SideNpcTalkType.Default:
                return;
            case SideNpcTalkType.Talk:
                Broadcast(TriggerPacket.SidePopupTalk(duration, illust, voice, script));
                return;
            case SideNpcTalkType.TalkBottom:
                return;
            case SideNpcTalkType.CutIn:
                Broadcast(TriggerPacket.SidePopupCutIn(duration, illust, voice, script));
                return;
            case SideNpcTalkType.Movie:
                return;
        }
    }

    public void WidgetAction(WidgetType type, string name, int widgetArgType, string args) { }

    #region Conditions
    public bool WidgetCondition(WidgetType type, string condition, string value) {
        return false;
    }
    #endregion
}
