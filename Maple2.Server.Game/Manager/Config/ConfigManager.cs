using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
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
    private readonly StatAttributes statAttributes;

    public readonly SkillManager Skill;

    public ConfigManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        keyBinds = new Dictionary<int, KeyBind>();
        hotBars = new List<HotBar>();
        skillMacros = new List<SkillMacro>();

        (
            IList<KeyBind>? KeyBinds,
            IList<QuickSlot[]>? HotBars,
            IList<SkillMacro>? Macros,
            IDictionary<StatAttribute, int>? Allocation,
            SkillBook? SkillBook
        ) load = db.LoadCharacterConfig(session.CharacterId);
        if (load.KeyBinds != null) {
            foreach (KeyBind keyBind in load.KeyBinds) {
                SetKeyBind(keyBind);
            }
        }
        for (int i = 0; i < TOTAL_HOT_BARS; i++) {
            hotBars.Add(new HotBar(load.HotBars?.ElementAtOrDefault(i)));
        }
        skillMacros = load.Macros ?? new List<SkillMacro>();

        statAttributes = new StatAttributes();
        if (load.Allocation != null) {
            foreach ((StatAttribute attribute, int amount) in load.Allocation) {
                statAttributes.Allocation[attribute] = amount;
                UpdateStatAttribute(attribute, amount, false);
            }
        }

        Skill = new SkillManager(session, load.SkillBook ?? new SkillBook());
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

    public void LoadStatAttributes() {
        session.Send(AttributePointPacket.Sources(statAttributes));
        session.Send(AttributePointPacket.Allocation(statAttributes));
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

    #region StatPoints
    public void AllocateStatPoint(StatAttribute type) {
        // Invalid stat type.
        if (StatAttributes.PointAllocation.StatLimit(type) <= 0) {
            return;
        }

        // No points remaining.
        if (statAttributes.UsedPoints >= statAttributes.TotalPoints) {
            return;
        }

        // Reached limit for allocation.
        if (session.Config.statAttributes.Allocation[type] >= StatAttributes.PointAllocation.StatLimit(type)) {
            session.Send(NoticePacket.Message("s_char_info_limit_stat_point"));
            return;
        }

        session.Config.statAttributes.Allocation[type]++;
        UpdateStatAttribute(type, 1);
        session.Send(AttributePointPacket.Allocation(session.Config.statAttributes));
    }

    public void ResetStatPoints() {
        foreach (StatAttribute type in session.Config.statAttributes.Allocation.Attributes) {
            int points = session.Config.statAttributes.Allocation[type];
            session.Config.statAttributes.Allocation[type] = 0;
            UpdateStatAttribute(type, -points);
        }

        session.Send(AttributePointPacket.Allocation(session.Config.statAttributes));
        session.Send(NoticePacket.Message("s_char_info_reset_stat_pointsuccess_msg"));
    }

    private void UpdateStatAttribute(StatAttribute type, int points, bool send = true) {
        switch (type) {
            case StatAttribute.Strength:
            case StatAttribute.Dexterity:
            case StatAttribute.Intelligence:
            case StatAttribute.Luck:
                session.Player.Stats[type].AddTotal(1 * points);
                break;
            case StatAttribute.Health:
                session.Player.Stats[StatAttribute.Health].AddTotal(10 * points);
                break;
            case StatAttribute.CriticalRate:
                session.Player.Stats[StatAttribute.CriticalRate].AddTotal(3 * points);
                break;
        }

        // Sends packet to notify client, skipped during loading.
        if (send) {
            session.Send(StatsPacket.Update(session.Player, type));
        }
    }
    #endregion

    public void Save(GameStorage.Request db) {
        db.SaveCharacterConfig(
            session.CharacterId, keyBinds.Values.ToList(),
            hotBars.Select(hotBar => hotBar.Slots).ToList(),
            skillMacros,
            statAttributes.Allocation,
            Skill.SkillBook
        );
    }
}
