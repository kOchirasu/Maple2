﻿// ReSharper disable InconsistentNaming, IdentifierTypo

using System.ComponentModel;

namespace Maple2.Model.Enum;

public enum ExpMessageCode {
    [Description("You got {0} experience.")]
    s_msg_take_exp = 0, // and 3004, 3007
    [Description("You got {0} experience from fishing.")]
    s_msg_take_fishing_exp = 1059,
    [Description("You got {0} experience for playing an instrument.")]
    s_msg_take_play_instrument_exp = 1063,
    [Description("You got {0} experience from Maple Arcade.")]
    s_msg_take_arcade_exp = 1071,
    [Description("You got {0} experience from opening a wooden treasure chest.")]
    s_msg_take_normal_chest_exp = 1091,
    [Description("You got {0} experience from opening a golden treasure chest.")]
    s_msg_take_normal_rare_exp = 1092,
    [Description("You got {0} experience from opening a golden treasure chest for the first time.")]
    s_msg_take_normal_rare_first_exp = 1093,
    [Description("You got {1} experience for discovering the {0} taxi stop.")]
    s_msg_take_taxi_exp = 3000,
    [Description("You got {1} experience for discovering {0}.")]
    s_msg_take_map_exp = 3001, // and 3002
    [Description("You got {1} experience for discovering a new area, {0}.")]
    s_msg_take_telescope_exp = 3003,
    [Description("You got {0} bonus experience.")]
    s_msg_take_assist_bonus_exp = 60002,
    [Description("You got an experience bonus for helping defeat a powerful foe. (EXP +{0})")]
    s_msg_take_assist_bonus_exp_system = 60002,
}
