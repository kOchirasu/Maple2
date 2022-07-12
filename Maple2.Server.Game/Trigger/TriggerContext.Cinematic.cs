using Maple2.Server.Game.Packets;
using Maple2.Trigger;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void AddCinematicTalk(int npcId, string illustId, string msg, int duration, Align align, int delayTick) {
        Broadcast(CinematicPacket.Talk(npcId, illustId, msg, delayTick, align));
    }

    public void RemoveCinematicTalk() {
        Broadcast(CinematicPacket.RemoveTalk());
    }

    public void CloseCinematic() {
        // guessing
        Broadcast(CinematicPacket.HideUi());
    }

    public void PlaySceneMovie(string fileName, int movieId, string skipType) {
        Broadcast(TriggerPacket.UiStartMovie(fileName, movieId));
    }

    public void SetCinematicIntro(string text) {
        Broadcast(CinematicPacket.Intro(text));
    }

    public void SetOnetimeEffect(int id, bool enabled, string path) {
        Broadcast(CinematicPacket.OneTimeEffect(id, enabled, path));
    }

    public void SetCinematicUI(byte type, string script, bool arg3) {
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

    public void SetSceneSkip(TriggerState? state, string nextState) { }

    public void SetSkip(TriggerState? state) { }

    public void AddBalloonTalk(int spawnId, string msg, int duration, int delayTick) {
        //CinematicPacket.BalloonTalk(false, 0, msg, duration, delayTick);
    }

    public void RemoveBalloonTalk(int spawnId) {
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
        Broadcast(CinematicPacket.Caption(type, title, script, align, offsetRateX, offsetRateY, duration, scale));
    }
}
