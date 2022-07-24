﻿using Maple2.Server.Game.Packets;
using Maple2.Trigger;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void AddCinematicTalk(int npcId, string illustId, string msg, int duration, Align align, int delayTick) {
        DebugLog("[AddCinematicTalk] npcId:{NpcId}, illustId:{IllustrationId}, msg:{Message}, duration:{Duration}, align:{Align}, delayTick:{Delay}",
            npcId, illustId, msg, duration, align, delayTick);
        Broadcast(CinematicPacket.Talk(npcId, illustId, msg, delayTick, align));
    }

    public void RemoveCinematicTalk() {
        DebugLog("[RemoveCinematicTalk]");
        Broadcast(CinematicPacket.RemoveTalk());
    }

    public void CloseCinematic() {
        DebugLog("[CloseCinematic]");
        // guessing
        Broadcast(CinematicPacket.HideUi());
    }

    public void PlaySceneMovie(string fileName, int movieId, string skipType) {
        DebugLog("[PlaySceneMovie] fileName:{FileName}, movieId:{MovieId}, skipType:{SkipType}", fileName, movieId, skipType);
        Broadcast(TriggerPacket.UiStartMovie(fileName, movieId));
    }

    public void SetCinematicIntro(string text) {
        DebugLog("[SetCinematicIntro] text:{Text}", text);
        Broadcast(CinematicPacket.Intro(text));
    }

    public void SetOnetimeEffect(int id, bool enabled, string path) {
        DebugLog("[SetOnetimeEffect] id:{Id}, enabled:{Enabled}, path:{Path}", id, enabled, path);
        Broadcast(CinematicPacket.OneTimeEffect(id, enabled, path));
    }

    public void SetCinematicUI(byte type, string script, bool arg3) {
        WarnLog("[SetCinematicUI] type:{Type}, script:{Script}, arg3:{Arg3}", type, script, arg3);
        switch (type) {
            case 0:
                Broadcast(CinematicPacket.ToggleUi(false));
                return;
            case 1:
                Broadcast(CinematicPacket.ToggleUi(true));
                return;
            case 2:
                Broadcast(CinematicPacket.HideUi());
                return;
            case 3:
                Broadcast(CinematicPacket.LetterboxTransition(script));
                return;
            case 4:
                Broadcast(CinematicPacket.FadeTransition(script));
                return;
            case 5:
                Broadcast(CinematicPacket.HorizontalTransition(script));
                return;
            case 6:
                Broadcast(CinematicPacket.VerticalTransition(script));
                return;
            case 7: // many
                return;
            case 9:
                Broadcast(CinematicPacket.Opening(script, arg3));
                return;
        }
    }

    public void SetSceneSkip(TriggerState? state, string nextState) {
        ErrorLog("[SetSceneSkip] state:{State}, nextState:{NextState}", nameof(state), nextState);
    }

    public void SetSkip(TriggerState? state) {
        ErrorLog("[SetSkip] state:{State}", nameof(state));
    }

    public void AddBalloonTalk(int spawnId, string msg, int duration, int delayTick) {
        ErrorLog("[AddBalloonTalk] spawnId:{SpawnId}, msg:{Message}, duration:{Duration}, delayTick:{Delay}", spawnId, msg, duration, delayTick);
        //CinematicPacket.BalloonTalk(false, 0, msg, duration, delayTick);
    }

    public void RemoveBalloonTalk(int spawnId) {
        ErrorLog("[RemoveBalloonTalk] spawnId:{SpawnId}", spawnId);
        //CinematicPacket.RemoveBalloonTalk(0);
    }

    public void ShowCaption(
        CaptionType type,
        string title,
        string script,
        Align align,
        float offsetRateX,
        float offsetRateY,
        int duration,
        float scale
    ) {
        DebugLog("[ShowCaption] type:{CaptionType}, title:{Title}, script:{Script}", type, title, script);
        Broadcast(CinematicPacket.Caption(type, title, script, align, offsetRateX, offsetRateY, duration, scale));
    }
}
