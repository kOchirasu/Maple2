using Maple2.Trigger;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void AddCinematicTalk(int npcId, string illustId, string msg, int duration, Align align, int delayTick) { }

    public void CloseCinematic() { }

    public void PlaySceneMovie(string fileName, int movieId, string skipType) { }

    public void RemoveCinematicTalk() { }

    public void SetCinematicIntro(string text) { }

    public void SetOnetimeEffect(int id, bool enable, string path) { }

    public void SetCinematicUI(byte type, string name, bool arg3) { }

    public void SetSceneSkip(TriggerState state, string nextState) { }

    public void SetSkip(TriggerState state) { }

    public void ShowCaption(CaptionType type, string title, string desc, Align align, float offsetRateX, float offsetRateY, int duration, float scale) { }
}
