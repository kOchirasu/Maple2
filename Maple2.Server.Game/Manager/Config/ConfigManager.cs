﻿using System.Diagnostics.CodeAnalysis;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Manager.Items;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager.Config;

public class ConfigManager {
    private const int TOTAL_HOT_BARS = 3;

    private readonly GameSession session;

    private readonly IDictionary<int, KeyBind> keyBinds;
    private short activeHotBar;
    private readonly List<HotBar> hotBars;
    private IList<SkillMacro> skillMacros;
    private IList<Wardrobe> wardrobes;
    private IList<int> favoriteStickers;
    private readonly IList<long> favoriteDesigners;
    private readonly IDictionary<LapenshardSlot, int> lapenshards;
    private readonly IDictionary<int, SkillCooldown> skillCooldowns;
    private long deathPenaltyTick;
    public int DeathCount;
    private readonly StatAttributes statAttributes;
    public readonly IDictionary<int, int> GatheringCounts;
    public readonly IDictionary<int, int> GuideRecords;

    public readonly SkillManager Skill;

    public ConfigManager(GameStorage.Request db, GameSession session) {
        this.session = session;
        keyBinds = new Dictionary<int, KeyBind>();
        hotBars = [];
        skillMacros = new List<SkillMacro>();
        lapenshards = new Dictionary<LapenshardSlot, int>();
        skillCooldowns = new Dictionary<int, SkillCooldown>();

        (
            IList<KeyBind>? KeyBinds,
            IList<QuickSlot[]>? HotBars,
            IList<SkillMacro>? Macros,
            IList<Wardrobe>? Wardrobes,
            IList<int>? FavoriteStickers,
            IList<long>? FavoriteDesigners,
            IDictionary<LapenshardSlot, int>? Lapenshards,
            IList<SkillCooldown>? SkillCooldowns,
            long deathPenaltyTick,
            int DeathCounter,
            IDictionary<BasicAttribute, int>? Allocation,
            IDictionary<int, int>? GatheringCounts,
            IDictionary<int, int>? GuideRecords,
        SkillBook? SkillBook
            ) load = db.LoadCharacterConfig(session.CharacterId);
        if (load.KeyBinds != null) {
            foreach (KeyBind keyBind in load.KeyBinds) {
                SetKeyBind(keyBind);
            }
        }
        Skill = new SkillManager(session, load.SkillBook ?? new SkillBook());

        for (int i = 0; i < TOTAL_HOT_BARS; i++) {
            hotBars.Add(new HotBar(load.HotBars?.ElementAtOrDefault(i)));
        }

        if (load.HotBars == null) {
            UpdateHotbarSkills();
        }

        skillMacros = load.Macros ?? new List<SkillMacro>();
        wardrobes = load.Wardrobes ?? new List<Wardrobe>();
        favoriteStickers = load.FavoriteStickers ?? new List<int>();
        favoriteDesigners = load.FavoriteDesigners ?? new List<long>();
        lapenshards = load.Lapenshards ?? new Dictionary<LapenshardSlot, int>();
        GatheringCounts = load.GatheringCounts ?? new Dictionary<int, int>();
        GuideRecords = load.GuideRecords ?? new Dictionary<int, int>();
        deathPenaltyTick = load.deathPenaltyTick;
        DeathCount = load.DeathCounter;

        if (load.SkillCooldowns != null) {
            foreach (SkillCooldown cooldown in load.SkillCooldowns) {
                if (Environment.TickCount64 > cooldown.EndTick) {
                    continue;
                }
                skillCooldowns[cooldown.SkillId] = cooldown;
            }
        }

        statAttributes = new StatAttributes();
        if (load.Allocation != null) {
            foreach ((BasicAttribute attribute, int amount) in load.Allocation) {
                statAttributes.Allocation[attribute] = amount;
                UpdateStatAttribute(attribute, amount, false);
            }
        }

    }

    public void UpdateHotbarSkills() {
        SortedDictionary<int, SkillInfo.Skill> skills = Skill.SkillInfo.GetMainLearnedJobSkills(SkillType.Active, SkillRank.Both);

        foreach (HotBar hotBar in hotBars) {
            foreach (QuickSlot quickSlot in hotBar.Slots) {
                if (quickSlot.SkillId > 0 && !skills.ContainsKey(quickSlot.SkillId)) {
                    hotBar.RemoveQuickSlot(quickSlot.SkillId, 0);
                }
            }
        }

        foreach ((int skillId, SkillInfo.Skill skill) in skills) {
            if (hotBars[activeHotBar].FindQuickSlotIndex(skillId) == -1) {
                hotBars[activeHotBar].MoveQuickSlot(-1, new QuickSlot(skillId), false);
            }
        }
        session.Send(KeyTablePacket.LoadHotBar(activeHotBar, hotBars));
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

    public void LoadWardrobe() {
        for (int i = 0; i < wardrobes.Count; i++) {
            session.Send(WardrobePacket.Load(i, wardrobes[i]));
        }
    }

    #region SkillCooldowns
    public void SaveSkillCooldown(SkillMetadata skill) {
        var cooldown = new SkillCooldown(skill.Id) {
            EndTick = (long) (skill.Data.Condition.CooldownTime * TimeSpan.FromSeconds(1).TotalMilliseconds) + Environment.TickCount64,
            OriginSkillId = skill.Data.Change?.Origin.Id ?? 0,
        };
        skillCooldowns[skill.Id] = cooldown;
    }

    public void SetSkillCooldown(int skillId, int endTick = 0) {
        var cooldown = new SkillCooldown(skillId) {
            EndTick = endTick,
        };
        skillCooldowns[skillId] = cooldown;

        session.Send(SkillPacket.Cooldown(cooldown));
    }

    public void LoadSkillCooldowns() {
        foreach ((int skillId, SkillCooldown cooldown) in skillCooldowns) {
            if (Environment.TickCount64 > cooldown.EndTick) {
                skillCooldowns.Remove(skillId);
            }
        }
        if (skillCooldowns.Count > 0) {
            session.Send(SkillPacket.Cooldown(skillCooldowns.Values.ToArray()));
        }
    }

    public IList<SkillCooldown> GetCurrentSkillCooldowns() {
        IList<SkillCooldown> cooldowns = new List<SkillCooldown>();
        foreach ((int skillId, SkillCooldown cooldown) in skillCooldowns) {
            if (Environment.TickCount64 > cooldown.EndTick) {
                continue;
            }
            cooldowns.Add(cooldown);
        }
        return cooldowns;
    }
    #endregion

    #region PremiumClub
    public void UpdatePremiumTime(long hours) {
        if (session.Player.Value.Account.PremiumTime < DateTime.Now.ToEpochSeconds()) {
            session.Player.Value.Account.PremiumTime = DateTime.Now.AddHours(hours).ToEpochSeconds();
            session.Send(NoticePacket.Notice(NoticePacket.Flags.Message | NoticePacket.Flags.Alert, StringCode.s_vip_coupon_new_msg));
        } else {
            session.Player.Value.Account.PremiumTime = Math.Min(session.Player.Value.Account.PremiumTime.FromEpochSeconds().AddHours(hours).ToEpochSeconds(), long.MaxValue);
            session.Send(NoticePacket.Notice(NoticePacket.Flags.Message | NoticePacket.Flags.Alert, StringCode.s_vip_coupon_extend_msg));
        }

        session.Send(PremiumCubPacket.Activate(session.Player.ObjectId, session.Player.Value.Account.PremiumTime));
        session.Player.Value.Character.PremiumTime = session.Player.Value.Account.PremiumTime;
        session.PlayerInfo.SendUpdate(new PlayerUpdateRequest {
            AccountId = session.AccountId,
            CharacterId = session.CharacterId,
            PremiumTime = session.Player.Value.Account.PremiumTime,
            Async = true,
        });
    }

    public void RefreshPremiumClubBuffs() {
        if (session.Player.Value.Account.PremiumTime > DateTime.Now.ToEpochSeconds()) {
            foreach ((int buffId, PremiumClubTable.Buff buff) in session.TableMetadata.PremiumClubTable.Buffs) {
                session.Player.Buffs.AddBuff(session.Player, session.Player, buff.Id, buff.Level);
            }
        }
    }

    public bool IsPremiumClubActive() => session.Player.Value.Account.PremiumTime > DateTime.Now.ToEpochSeconds();
    #endregion

    #region ChatStickers
    public void LoadChatStickers() {
        List<ChatSticker> stickers = [];
        foreach (KeyValuePair<int, long> set in session.Player.Value.Unlock.StickerSets) {
            stickers.Add(new(set.Key, set.Value));
        }
        session.Send(ChatStickerPacket.Load(favoriteStickers.ToList(), stickers));
    }

    public bool TryFavoriteChatSticker(int stickerId) {
        if (favoriteStickers.Contains(stickerId)) {
            return false;
        }

        favoriteStickers.Add(stickerId);
        return true;
    }

    public bool TryUnfavoriteChatSticker(int stickerId) {
        if (!favoriteStickers.Contains(stickerId)) {
            return false;
        }

        favoriteStickers.Remove(stickerId);
        return true;
    }
    #endregion

    public void LoadStatAttributes() {
        session.Send(AttributePointPacket.Sources(statAttributes));
        session.Send(AttributePointPacket.Allocation(statAttributes));
    }

    public IList<long> GetFavoriteDesigners() {
        return favoriteDesigners;
    }

    public void AddFavoriteDesigner(long designer) {
        favoriteDesigners.Add(designer);
    }

    public void RemoveFavoriteDesigner(long designer) {
        favoriteDesigners.Remove(designer);
    }

    public void UpdateDeathPenalty(int tick) {
        deathPenaltyTick = tick;
        DeathCount = tick > 0 ? DeathCount++ : 0;

        session.Send(RevivalPacket.Confirm(session.Player, (int) deathPenaltyTick, DeathCount));
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

    #region Wardrobe
    public bool AddWardrobe(string name) {
        if (wardrobes.Count >= Constant.MaxClosetMaxCount) {
            return false;
        }
        if (name.Length > Constant.MaxClosetTabNameLength) {
            return false;
        }

        wardrobes.Add(new Wardrobe(0, name));
        return true;
    }

    public bool TryGetWardrobe(int index, [NotNullWhen(true)] out Wardrobe? wardrobe) {
        if (index < 0 || index >= wardrobes.Count) {
            wardrobe = null;
            return false;
        }

        wardrobe = wardrobes[index];
        return true;
    }
    #endregion

    #region StatPoints
    public void AllocateStatPoint(BasicAttribute type) {
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
        foreach (BasicAttribute type in session.Config.statAttributes.Allocation.Attributes) {
            int points = session.Config.statAttributes.Allocation[type];
            session.Config.statAttributes.Allocation[type] = 0;
            UpdateStatAttribute(type, -points);
        }

        session.Send(AttributePointPacket.Allocation(session.Config.statAttributes));
        session.Send(NoticePacket.Message("s_char_info_reset_stat_pointsuccess_msg"));
    }

    private void UpdateStatAttribute(BasicAttribute type, int points, bool send = true) {
        switch (type) {
            case BasicAttribute.Strength:
            case BasicAttribute.Dexterity:
            case BasicAttribute.Intelligence:
            case BasicAttribute.Luck:
                session.Player.Stats[type].AddTotal(1 * points);
                break;
            case BasicAttribute.Health:
                session.Player.Stats[BasicAttribute.Health].AddTotal(10 * points);
                break;
            case BasicAttribute.CriticalRate:
                session.Player.Stats[BasicAttribute.CriticalRate].AddTotal(3 * points);
                break;
        }

        // Sends packet to notify client, skipped during loading.
        if (send) {
            session.Send(StatsPacket.Update(session.Player, type));
        }
    }
    #endregion

    #region Lapenshard
    public void LoadLapenshard() {
        session.Send(LapenshardPacket.Load(lapenshards));
    }

    public int TryGetLapenshard(LapenshardSlot slot) {
        return lapenshards.TryGetValue(slot, out int id) ? id : -1;
    }

    public void SetLapenshard(LapenshardSlot slot, int id) {
        lapenshards[slot] = id;
    }

    public bool EquipLapenshard(long itemUid, LapenshardSlot slot) {
        if (!Enum.IsDefined<LapenshardSlot>(slot)) {
            return false;
        }

        Item? lapenshard = session.Item.Inventory.Get(itemUid);
        if (lapenshard == null) {
            session.Send(NoticePacket.MessageBox(StringCode.s_item_invalid_do_not_have));
            return false;
        }
        if (!lapenshard.Type.IsLapenshard) {
            return false;
        }
        StringCode result = EquipManager.ValidateEquipItem(session, lapenshard);
        if (result != StringCode.s_empty_string) {
            session.Send(NoticePacket.MessageBox(result));
            return false;
        }

        // Cannot equip the same lapenshard twice.
        foreach (LapenshardSlot lapenshardSlot in Enum.GetValues<LapenshardSlot>()) {
            if (lapenshards.TryGetValue(lapenshardSlot, out int existing) && existing == lapenshard.Id) {
                return false;
            }
        }

        if (lapenshard.Type.IsRedLapenshard && slot is not (LapenshardSlot.Red1 or LapenshardSlot.Red2)) {
            return false;
        }
        if (lapenshard.Type.IsBlueLapenshard && slot is not (LapenshardSlot.Blue1 or LapenshardSlot.Blue2)) {
            return false;
        }
        if (lapenshard.Type.IsGreenLapenshard && slot is not (LapenshardSlot.Green1 or LapenshardSlot.Green2)) {
            return false;
        }

        if (!UnequipLapenshard(slot)) {
            return false;
        }

        if (!session.Item.Inventory.Consume(itemUid, 1)) {
            return false;
        }

        lapenshards[slot] = lapenshard.Id;
        session.Send(LapenshardPacket.Equip(slot, lapenshard.Id));

        return true;
    }

    public bool UnequipLapenshard(LapenshardSlot slot) {
        if (!Enum.IsDefined<LapenshardSlot>(slot)) {
            return false;
        }

        if (!lapenshards.TryGetValue(slot, out int lapenshardId)) {
            return true; // Nothing to unequip
        }

        Item? lapenshard = session.Item.CreateItem(lapenshardId, Constant.LapenshardGrade);
        if (lapenshard == null) {
            return false;
        }

        if (!lapenshards.Remove(slot, out lapenshardId)) {
            return false;
        }

        lapenshard.Transfer?.Bind(session.Player.Value.Character);
        bool success = session.Item.Inventory.Add(lapenshard);
        if (!success) {
            lapenshards[slot] = lapenshardId; // Revert removal
            return false;
        }

        session.Send(LapenshardPacket.Unequip(slot));
        return true;
    }
    #endregion

    public void Save(GameStorage.Request db) {
        db.SaveCharacterConfig(
            session.CharacterId, keyBinds.Values.ToList(),
            hotBars.Select(hotBar => hotBar.Slots).ToList(),
            skillMacros,
            wardrobes,
            favoriteStickers,
            favoriteDesigners,
            lapenshards,
            skillCooldowns.Values.ToList(),
            deathPenaltyTick,
            DeathCount,
            statAttributes.Allocation,
            GatheringCounts,
            GuideRecords,
            Skill.SkillBook
        );
    }
}
