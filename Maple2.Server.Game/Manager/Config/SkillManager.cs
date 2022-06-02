using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Constants;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Config;

public class SkillManager {
    private readonly GameSession session;

    public readonly SkillBook SkillBook;
    public readonly SkillInfo SkillInfo;

    public SkillManager(GameSession session, SkillBook skillBook) {
        this.session = session;

        SkillBook = skillBook;
        SkillInfo = new SkillInfo(session, GetSkillTab(skillBook.ActiveSkillTabId));
    }

    public void LoadSkillBook() {
        session.Send(SkillBookPacket.Load(SkillBook));
    }

    #region SkillBook
    public SkillTab? GetSkillTab(long id) {
        return SkillBook.SkillTabs.SingleOrDefault(skillTab => skillTab.Id == id);
    }

    public bool SaveSkillTab(long activeSkillTabId, SkillRank ranks, SkillTab? tab = null) {
        if (GetSkillTab(activeSkillTabId) != null) {
            SkillBook.ActiveSkillTabId = activeSkillTabId;
        }

        // Switching Active Tab
        if (tab == null) {
            SkillTab? activeTab = GetSkillTab(SkillBook.ActiveSkillTabId);
            if (activeTab != null) {
                SkillInfo.SetTab(activeTab);
            }
            session.Send(SkillBookPacket.Save(SkillBook, 0, ranks));
            return true;
        }

        // AddOrUpdate SkillTab
        SkillTab? existingTab = GetSkillTab(tab.Id);
        if (existingTab == null) {
            // Need to create a new tab
            bool result = CreateSkillTab(tab);
            if (result) {
                session.Send(SkillBookPacket.Save(SkillBook, tab.Id, ranks));
            }
            return result;
        }

        foreach (SkillTab.Skill entry in tab.Skills) {
            SkillInfo.Skill? skill = SkillInfo.GetSkill(entry.SkillId, ranks);
            if (skill != null) {
                existingTab.AddOrUpdate(entry);
            }
        }

        session.Send(SkillBookPacket.Save(SkillBook, tab.Id, ranks));
        return true;
    }

    public bool ExpandSkillTabs() {
        if (SkillBook.SkillTabs.Count < SkillBook.MaxSkillTabs) {
            return true;
        }

        if (SkillBook.MaxSkillTabs >= Constant.MaxSkillTabCount) {
            return false;
        }
        if (session.Currency.Meret < Constant.SkillBookTreeAddTabFeeMeret) {
            return false;
        }

        session.Currency.Meret -= Constant.SkillBookTreeAddTabFeeMeret;
        SkillBook.MaxSkillTabs++;
        session.Send(SkillBookPacket.Expand(SkillBook));

        return true;
    }

    private bool CreateSkillTab(SkillTab skillTab) {
        using GameStorage.Request db = session.GameStorage.Context();
        if (SkillBook.SkillTabs.Count >= SkillBook.MaxSkillTabs) {
            return false;
        }

        skillTab = db.CreateSkillTab(session.CharacterId, skillTab);
        if (skillTab == null) {
            return false;
        }

        SkillBook.SkillTabs.Add(skillTab);
        return true;
    }
    #endregion

    #region SkillInfo
    public void UpdateSkill(int skillId, short level, bool enabled) {
        SkillInfo.Skill? skill = SkillInfo.GetSkill(skillId);
        if (skill == null) {
            return;
        }

        // Level must be set to 0 if not enabled since there is a placeholder value of 1.
        if (!enabled) {
            skill.Level = 0;
            return;
        }

        if (skill.Level == skill.BaseLevel && level > skill.BaseLevel) {
            skill.Notify = true;
        }

        skill.Level = level;
    }
    #endregion

    public void ResetSkills(SkillRank rank = SkillRank.Both) {
        foreach (SkillType type in Enum.GetValues(typeof(SkillType))) {
            foreach (SkillInfo.Skill skill in SkillInfo.GetSkills(type, rank)) {
                skill.Level = skill.BaseLevel;
            }
        }
    }
}
