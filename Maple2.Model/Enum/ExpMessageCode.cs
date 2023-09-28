// ReSharper disable InconsistentNaming, IdentifierTypo

using System.ComponentModel;

namespace Maple2.Model.Enum;

public enum ExpMessageCode : ushort {
    [Description("s_msg_take_exp - You got {0} experience.")]
    none = 0,
    [Description("s_msg_take_exp - You got {0} experience.")]
    quest = 1010,
    [Description("s_msg_take_exp - You got {0} experience.")]
    mission = 1053,
    [Description("s_msg_take_fishing_exp - You got {0} experience from fishing.")]
    fishing = 1059,
    [Description("s_msg_take_exp - You got {0} experience.")]
    expDrop = 1062,
    [Description("s_msg_take_play_instrument_exp - You got {0} experience for playing an instrument.")]
    musicMastery = 1063,
    [Description("s_msg_take_arcade_exp - You got {0} experience from Maple Arcade.")]
    arcade = 1071,
    [Description("s_msg_take_exp - You got {0} experience.")]
    dungeonRelative = 1085,
    [Description("s_msg_take_normal_chest_exp - You got {0} experience from opening a wooden treasure chest.")]
    normalChest = 1091,
    [Description("s_msg_take_normal_rare_exp - You got {0} experience from opening a golden treasure chest.")]
    rareChest = 1092,
    [Description("s_msg_take_normal_rare_first_exp - You got {0} experience from opening a golden treasure chest for the first time.")]
    rareChestFirst = 1093,
    [Description("s_msg_take_taxi_exp - You got {1} experience for discovering the {0} taxi stop.")]
    taxi = 3000,
    [Description("s_msg_take_map_exp - You got {1} experience for discovering {0}.")]
    mapCommon = 3001,
    [Description("s_msg_take_map_exp - You got {1} experience for discovering {0}.")]
    mapHidden = 3002, // Maps not displayed in the world map
    [Description("s_msg_take_telescope_exp - You got {1} experience for discovering a new area, {0}.")]
    telescope = 3003,
    [Description("s_msg_take_exp - You got {0} experience.")]
    miniGame = 3007,
    [Description("s_msg_take_exp - You got {0} experience.")]
    gathering = 3103,
    [Description("s_msg_take_exp - You got {0} experience.")]
    manufacturing = 3107,
    [Description("s_msg_take_exp - You got {0} experience.")]
    petTaming = 3200,
    [Description("s_msg_take_exp - You got {0} experience.")]
    guildUserExp = 3301,

    //TODO: Find values of these
    rest,
    bloodMineRank1,
    bloodMineRank2,
    bloodMineRank3,
    bloodMineRankOther,
    redDuelWin,
    redDuelLose,
    btiTeamWin,
    btiTeamLose,
    rankDuelWin,
    rankDuelLose,
    userMiniGameExtra,
    construct,
    mapleSurvival,
    dailyGuildQuest,
    weeklyGuildQuest,
    dailymission,
    dailymissionLevelUp,
    randomDungeonBonus,

    // These names are guesses and not part of the exp type table
    [Description("s_msg_take_exp - You got {0} experience.")]
    monster = 60001,
    [Description("s_msg_take_assist_bonus_exp - You got {0} bonus experience.")]
    assist = 60002,
    [Description("s_msg_take_assist_bonus_exp_system - You got an experience bonus for helping defeat a powerful foe. (EXP +{0})")]
    assistBonus = 60003,
}
