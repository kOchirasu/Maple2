using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Core.Constants;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Config;

public class ConfigManager {
    private const int TOTAL_HOT_BARS = 3;

    private readonly GameSession session;

    private readonly Dictionary<int, KeyBind> keyBinds;
    private short activeHotBar;
    private readonly List<HotBar> hotBars;
    private IList<SkillMacro> skillMacros;
    private readonly SkillBook skillBook;

    public ConfigManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        keyBinds = new Dictionary<int, KeyBind>();
        hotBars = new List<HotBar>();
        skillMacros = new List<SkillMacro>();

        (IList<KeyBind>? KeyBinds, IList<QuickSlot[]>? HotBars, IList<SkillMacro>? Macros, SkillBook? SkillBook) load =
            db.LoadCharacterConfig(session.CharacterId);
        if (load.KeyBinds != null) {
            foreach (KeyBind keyBind in load.KeyBinds) {
                SetKeyBind(keyBind);
            }
        }
        for (int i = 0; i < TOTAL_HOT_BARS; i++) {
            hotBars.Add(new HotBar(load.HotBars?.ElementAtOrDefault(i)));
        }
        skillMacros = load.Macros ?? new List<SkillMacro>();
        skillBook = load.SkillBook ?? new SkillBook();
    }

    public void LoadKeyTable() {
        session.Send(keyBinds.Count == 0
            ? KeyTablePacket.LoadDefault()
            : KeyTablePacket.Load(keyBinds.Values, activeHotBar, hotBars));
    }

    public void LoadHotBars() {
        session.Send(KeyTablePacket.LoadHotBar(activeHotBar, hotBars));
    }

    public void LoadMacros() {
        session.Send(SkillMacroPacket.Load(skillMacros));
    }

    public void LoadSkillBook() {
        session.Send(SkillBookPacket.Load(skillBook));
    }

    #region KeyBind
    public void SetKeyBind(in KeyBind keyBind) {
        keyBinds[keyBind.KeyCode] = keyBind;
    }
    #endregion

    #region HotBar
    public void SetActiveHotBar(short hotBarId) {
        if (hotBarId < 0 || hotBarId >= hotBars.Count) {
            return;
        }

        activeHotBar = hotBarId;
    }

    public bool TryGetHotBar(short hotBarId, [NotNullWhen(true)] out HotBar? hotBar) {
        if (hotBarId < 0 || hotBarId >= hotBars.Count) {
            hotBar = null;
            return false;
        }

        hotBar = hotBars[hotBarId];
        return true;
    }
    #endregion

    #region Macros
    public void UpdateMacros(List<SkillMacro> updated) {
        skillMacros = updated;
    }
    #endregion

    #region SkillBook
    public SkillTab? GetSkillTab(long id) {
        return skillBook.SkillTabs.SingleOrDefault(skillTab => skillTab.Id == id);
    }

    public bool SaveSkillTab(long activeSkillTabId, SkillRank ranks, SkillTab? tab = null) {
        if (GetSkillTab(activeSkillTabId) != null) {
            skillBook.ActiveSkillTabId = activeSkillTabId;
        }

        if (tab == null) {
            session.Send(SkillBookPacket.Save(skillBook, 0, ranks));
            return true;
        }

        // AddOrUpdate SkillTab
        SkillTab? existingTab = GetSkillTab(tab.Id);
        if (existingTab == null) {
            // Need to create a new tab
            using GameStorage.Request db = session.GameStorage.Context();
            return CreateSkillTab(db, new SkillTab(string.Empty));
        }

        if (ranks == SkillRank.Both) {
            existingTab.Skills = tab.Skills;
            return true;
        }

        foreach (SkillTab.Skill entry in tab.Skills) {
            JobInfo.Skill? skill = session.Skill.JobInfo.GetSkill(entry.SkillId, ranks);
            if (skill != null) {
                existingTab.AddOrUpdate(entry);
            }
        }

        session.Send(SkillBookPacket.Save(skillBook, tab.Id, ranks));
        return true;
    }

    public bool ExpandSkillTabs() {
        if (skillBook.SkillTabs.Count < skillBook.MaxSkillTabs) {
            return true;
        }

        if (skillBook.MaxSkillTabs >= Constant.MaxSkillTabCount) {
            return false;
        }
        if (session.Currency.Meret < Constant.SkillBookTreeAddTabFeeMeret) {
            return false;
        }

        session.Currency.Meret -= Constant.SkillBookTreeAddTabFeeMeret;
        skillBook.MaxSkillTabs++;
        session.Send(SkillBookPacket.Expand(skillBook));

        return true;
    }

    private bool CreateSkillTab(GameStorage.Request db, SkillTab skillTab) {
        if (skillBook.SkillTabs.Count >= skillBook.MaxSkillTabs) {
            return false;
        }

        skillTab = db.CreateSkillTab(session.CharacterId, skillTab);
        if (skillTab == null) {
            return false;
        }

        skillBook.ActiveSkillTabId = skillTab.Id;
        skillBook.SkillTabs.Add(skillTab);
        return true;
    }
    #endregion

    public void Save(GameStorage.Request db) {
        db.SaveCharacterConfig(
            session.CharacterId, keyBinds.Values.ToList(),
            hotBars.Select(hotBar => hotBar.Slots).ToList(),
            skillMacros,
            skillBook
        );
    }
}
