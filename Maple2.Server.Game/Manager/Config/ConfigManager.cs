using System.Collections.Generic;
using Maple2.Model.Game;

namespace Maple2.Server.Game.Manager.Config;

public class ConfigManager {
    private const int TOTAL_HOT_BARS = 3;

    private readonly Dictionary<int, KeyBind> keyBinds;
    private readonly List<HotBar> hotBars;

    public ConfigManager() {
        keyBinds = new Dictionary<int, KeyBind>();
        hotBars = new List<HotBar>();

        for (int i = 0; i < TOTAL_HOT_BARS; i++) {
            hotBars.Add(new HotBar());
        }
    }

    #region KeyBind
    public ICollection<KeyBind> KeyBinds => keyBinds.Values;

    public void SetKeyBind(in KeyBind keyBind) {
        keyBinds[keyBind.KeyCode] = keyBind;
    }
    #endregion

    #region HotBar
    public short ActiveHotBar { get; private set; }
    public IReadOnlyList<HotBar> HotBars => hotBars;

    public void SetActiveHotBar(short hotBarId) {
        if (hotBarId < 0 || hotBarId >= hotBars.Count) {
            return;
        }

        ActiveHotBar = hotBarId;
    }

    public bool TryGetHotBar(short hotBarId, out HotBar? hotBar) {
        if (hotBarId < 0 || hotBarId >= hotBars.Count) {
            hotBar = null;
            return false;
        }

        hotBar = hotBars[hotBarId];
        return true;
    }
    #endregion
}
