﻿using Maple2.Model.Enum;

namespace Maple2.File.Ingest.Utils;

internal static class AttributeExtensions {
    public static BasicAttribute ToBasicAttribute(this string value) {
        return value.ToLower() switch {
            "str" => BasicAttribute.Strength,
            "dex" => BasicAttribute.Dexterity,
            "int" => BasicAttribute.Intelligence,
            "luk" => BasicAttribute.Luck,
            "hp" => BasicAttribute.Health,
            "hp_rgp" => BasicAttribute.HpRegen,
            "hp_inv" => BasicAttribute.HpRegenInterval,
            "sp" => BasicAttribute.Spirit,
            "sp_rgp" => BasicAttribute.SpRegen,
            "sp_inv" => BasicAttribute.SpRegenInterval,
            "ep" => BasicAttribute.Stamina,
            "ep_rgp" => BasicAttribute.StaminaRegen,
            "ep_inv" => BasicAttribute.StaminaRegenInterval,
            "asp" => BasicAttribute.AttackSpeed,
            "msp" => BasicAttribute.MovementSpeed,
            "atp" => BasicAttribute.Accuracy,
            "evp" => BasicAttribute.Evasion,
            "cap" => BasicAttribute.CriticalRate,
            "cad" => BasicAttribute.CriticalDamage,
            "car" => BasicAttribute.CriticalEvasion,
            "ndd" => BasicAttribute.Defense,
            "abp" => BasicAttribute.PerfectGuard,
            "jmp" => BasicAttribute.JumpHeight,
            "pap" => BasicAttribute.PhysicalAtk,
            "map" => BasicAttribute.MagicalAtk,
            "par" => BasicAttribute.PhysicalRes,
            "mar" => BasicAttribute.MagicalRes,
            "wapmin" => BasicAttribute.MinWeaponAtk,
            "wapmax" => BasicAttribute.MaxWeaponAtk,
            "dmg" => BasicAttribute.Damage,
            "pen" => BasicAttribute.Piercing,
            "rmsp" => BasicAttribute.MountSpeed,
            "bap" => BasicAttribute.BonusAtk,
            "bap_pet" => BasicAttribute.PetBonusAtk,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid BasicAttribute."),
        };
    }

    public static SpecialAttribute ToSpecialAttribute(this string value) {
        return value.ToLower() switch {
            "seg" => SpecialAttribute.Experience,
            "smd" => SpecialAttribute.Meso,
            "sss" => SpecialAttribute.SwimSpeed,
            "dashdistance" => SpecialAttribute.DashDistance,
            // "spd" => 4,
            // "sid" => 5,
            "finaladditionaldamage" => SpecialAttribute.TotalDamage,
            "cri" => SpecialAttribute.CriticalDamage,
            "sgi" => SpecialAttribute.NormalNpcDamage,
            "sgi_leader" => SpecialAttribute.LeaderNpcDamage,
            "sgi_elite" => SpecialAttribute.EliteNpcDamage,
            "sgi_boss" => SpecialAttribute.BossNpcDamage,
            "killhprestore" => SpecialAttribute.HpOnKill,
            "killsprestore" => SpecialAttribute.SpiritOnKill,
            "killeprestore" => SpecialAttribute.StaminaOnKill,
            "heal" => SpecialAttribute.RecoveryBonus,
            "receivedhealincrease" => SpecialAttribute.BonusRecoveryFromAlly,
            "icedamage" => SpecialAttribute.IceDamage,
            "firedamage" => SpecialAttribute.FireDamage,
            "darkdamage" => SpecialAttribute.DarkDamage,
            "lightdamage" => SpecialAttribute.HolyDamage,
            "poisondamage" => SpecialAttribute.PoisonDamage,
            "thunderdamage" => SpecialAttribute.ElectricDamage,
            "nddincrease" => SpecialAttribute.MeleeDamage,
            "lddincrease" => SpecialAttribute.RangedDamage,
            "parpen" => SpecialAttribute.PhysicalPiercing,
            "marpen" => SpecialAttribute.MagicalPiercing,
            "icedamagereduce" => SpecialAttribute.ReduceIceDamage,
            "firedamagereduce" => SpecialAttribute.ReduceFireDamage,
            "darkdamagereduce" => SpecialAttribute.ReduceDarkDamage,
            "lightdamagereduce" => SpecialAttribute.ReduceHolyDamage,
            "poisondamagereduce" => SpecialAttribute.ReducePoisonDamage,
            "thunderdamagereduce" => SpecialAttribute.ReduceElectricDamage,
            "stunreduce" => SpecialAttribute.ReduceStun,
            "conditionreduce" => SpecialAttribute.ReduceDebuff,
            "skillcooldown" => SpecialAttribute.ReduceCooldown,
            "neardistancedamagereduce" => SpecialAttribute.ReduceMeleeDamage,
            "longdistancedamagereduce" => SpecialAttribute.ReduceRangedDamage,
            "knockbackreduce" => SpecialAttribute.ReduceKnockBack,
            "stunprocndd" => SpecialAttribute.MeleeStun,
            "stunprocldd" => SpecialAttribute.RangedStun,
            "knockbackprocndd" => SpecialAttribute.MeleeKnockBack,
            "knockbackprocldd" => SpecialAttribute.RangedKnockBack,
            "snareprocndd" => SpecialAttribute.MeleeImmobilize,
            "snareprocldd" => SpecialAttribute.RangedImmobilize,
            "aoeprocndd" => SpecialAttribute.MeleeSplashDamage,
            "aoeprocldd" => SpecialAttribute.RangedSplashDamage,
            "npckilldropitemincrate" => SpecialAttribute.DropRate,
            "seg_questreward" => SpecialAttribute.QuestExp,
            "smd_questreward" => SpecialAttribute.QuestMeso,
            "seg_fishingreward" => SpecialAttribute.FishingExp,
            "seg_arcadereward" => SpecialAttribute.ArcadeExp,
            "seg_playinstrumentreward" => SpecialAttribute.PlayInstrumentExp,
            "invoke_effect1" => SpecialAttribute.InvokeEffect1,
            "invoke_effect2" => SpecialAttribute.InvokeEffect2,
            "invoke_effect3" => SpecialAttribute.InvokeEffect3,
            "pvpdamageincrease" => SpecialAttribute.PvpDamage,
            "pvpdamagereduce" => SpecialAttribute.ReducePvpDamage,
            "improveguildexp" => SpecialAttribute.GuildExp,
            "improveguildcoin" => SpecialAttribute.GuildCoin,
            "improvemassiveeventbexpball" => SpecialAttribute.MassiveEventExpBall,
            "reduce_meso_trade_fee" => SpecialAttribute.ReduceMesoTradeFee,
            "reduce_enchant_matrial_fee" => SpecialAttribute.ReduceEnchantMaterialFee,
            "reduce_merat_revival_fee" => SpecialAttribute.ReduceMeretRevivalFee,
            "improve_mining_reward_item" => SpecialAttribute.MiningRewardItem,
            "improve_breeding_reward_item" => SpecialAttribute.BreedingRewardItem,
            "improve_blacksmithing_reward_mastery" => SpecialAttribute.SmithingRewardMastery,
            "improve_engraving_reward_mastery" => SpecialAttribute.EngravingRewardMastery,
            "improve_gathering_reward_item" => SpecialAttribute.GatheringRewardItem,
            "improve_farming_reward_item" => SpecialAttribute.FarmingRewardItem,
            "improve_alchemist_reward_mastery" => SpecialAttribute.AlchemistRewardMastery,
            "improve_cooking_reward_mastery" => SpecialAttribute.CookingRewardMastery,
            "improve_acquire_gathering_exp" => SpecialAttribute.AcquireGatheringExp,
            "skill_levelup_tier_1" => SpecialAttribute.SkillLevelUpTier1,
            "skill_levelup_tier_2" => SpecialAttribute.SkillLevelUpTier2,
            "skill_levelup_tier_3" => SpecialAttribute.SkillLevelUpTier3,
            "skill_levelup_tier_4" => SpecialAttribute.SkillLevelUpTier4,
            "skill_levelup_tier_5" => SpecialAttribute.SkillLevelUpTier5,
            "skill_levelup_tier_6" => SpecialAttribute.SkillLevelUpTier6,
            "skill_levelup_tier_7" => SpecialAttribute.SkillLevelUpTier7,
            "skill_levelup_tier_8" => SpecialAttribute.SkillLevelUpTier8,
            "skill_levelup_tier_9" => SpecialAttribute.SkillLevelUpTier9,
            "skill_levelup_tier_10" => SpecialAttribute.SkillLevelUpTier10,
            "skill_levelup_tier_11" => SpecialAttribute.SkillLevelUpTier11,
            "skill_levelup_tier_12" => SpecialAttribute.SkillLevelUpTier12,
            "skill_levelup_tier_13" => SpecialAttribute.SkillLevelUpTier13,
            "skill_levelup_tier_14" => SpecialAttribute.SkillLevelUpTier14,
            "improve_massive_ox_exp" => SpecialAttribute.MassiveOxExp,
            "improve_massive_trapmaster_exp" => SpecialAttribute.MassiveTrapMasterExp,
            "improve_massive_finalsurvival_exp" => SpecialAttribute.MassiveFinalSurvivalExp,
            "improve_massive_crazyrunner_exp" => SpecialAttribute.MassiveCrazyRunnerExp,
            "improve_massive_sh_crazyrunner_exp" => SpecialAttribute.MassiveShCrazyRunnerExp,
            "improve_massive_escape_exp" => SpecialAttribute.MassiveEscapeExp,
            "improve_massive_springbeach_exp" => SpecialAttribute.MassiveSpringBeachExp,
            "improve_massive_dancedance_exp" => SpecialAttribute.MassiveDanceDanceExp,
            "improve_massive_ox_msp" => SpecialAttribute.MassiveOxSpeed,
            "improve_massive_trapmaster_msp" => SpecialAttribute.MassiveTrapMasterSpeed,
            "improve_massive_finalsurvival_msp" => SpecialAttribute.MassiveFinalSurvivalSpeed,
            "improve_massive_crazyrunner_msp" => SpecialAttribute.MassiveCrazyRunnerSpeed,
            "improve_massive_sh_crazyrunner_msp" => SpecialAttribute.MassiveShCrazyRunnerSpeed,
            "improve_massive_escape_msp" => SpecialAttribute.MassiveEscapeSpeed,
            "improve_massive_springbeach_msp" => SpecialAttribute.MassiveSpringBeachSpeed,
            "improve_massive_dancedance_msp" => SpecialAttribute.MassiveDanceDanceSpeed,
            "npc_hit_reward_sp_ball" => SpecialAttribute.NpcHitRewardSpBall,
            "npc_hit_reward_ep_ball" => SpecialAttribute.NpcHitRewardEpBall,
            "improve_honor_token" => SpecialAttribute.HonorToken,
            "improve_pvp_exp" => SpecialAttribute.PvpExp,
            "improve_darkstream_damage" => SpecialAttribute.DarkStreamDamage,
            "reduce_darkstream_recive_damage" => SpecialAttribute.ReduceDarkStreamReceiveDamage,
            "improve_darkstream_evp" => SpecialAttribute.DarkStreamEvp,
            "fishing_double_mastery" => SpecialAttribute.FishingDoubleMastery,
            "playinstrument_double_mastery" => SpecialAttribute.PlayInstrumentDoubleMastery,
            "complete_fieldmission_msp" => SpecialAttribute.CompleteFieldMissionSpeed,
            "improve_glide_vertical_velocity" => SpecialAttribute.GlideVerticalVelocity,
            "additionaleffect_95000018" => SpecialAttribute.AdditionalEffect95000018,
            "additionaleffect_95000012" => SpecialAttribute.AdditionalEffect95000012,
            "additionaleffect_95000014" => SpecialAttribute.AdditionalEffect95000014,
            "additionaleffect_95000020" => SpecialAttribute.AdditionalEffect95000020,
            "additionaleffect_95000021" => SpecialAttribute.AdditionalEffect95000021,
            "additionaleffect_95000022" => SpecialAttribute.AdditionalEffect95000022,
            "additionaleffect_95000023" => SpecialAttribute.AdditionalEffect95000023,
            "additionaleffect_95000024" => SpecialAttribute.AdditionalEffect95000024,
            "additionaleffect_95000025" => SpecialAttribute.AdditionalEffect95000025,
            "additionaleffect_95000026" => SpecialAttribute.AdditionalEffect95000026,
            "additionaleffect_95000027" => SpecialAttribute.AdditionalEffect95000027,
            "additionaleffect_95000028" => SpecialAttribute.AdditionalEffect95000028,
            "additionaleffect_95000029" => SpecialAttribute.AdditionalEffect95000029,
            "reduce_recovery_ep_inv" => SpecialAttribute.ReduceRecoveryEpInv,
            "improve_stat_wap_u" => SpecialAttribute.MaxWeaponAttack,
            "mining_double_reward" => SpecialAttribute.MiningDoubleReward,
            "breeding_double_reward" => SpecialAttribute.BreedingDoubleReward,
            "gathering_double_reward" => SpecialAttribute.GatheringDoubleReward,
            "farming_double_reward" => SpecialAttribute.FarmingDoubleReward,
            "blacksmithing_double_reward" => SpecialAttribute.SmithingDoubleReward,
            "engraving_double_reward" => SpecialAttribute.EngravingDoubleReward,
            "alchemist_double_reward" => SpecialAttribute.AlchemistDoubleReward,
            "cooking_double_reward" => SpecialAttribute.CookingDoubleReward,
            "mining_double_mastery" => SpecialAttribute.MiningDoubleMastery,
            "breeding_double_mastery" => SpecialAttribute.BreedingDoubleMastery,
            "gathering_double_mastery" => SpecialAttribute.GatheringDoubleMastery,
            "farming_double_mastery" => SpecialAttribute.FarmingDoubleMastery,
            "blacksmithing_double_mastery" => SpecialAttribute.SmithingDoubleMastery,
            "engraving_double_mastery" => SpecialAttribute.EngravingDoubleMastery,
            "alchemist_double_mastery" => SpecialAttribute.AlchemistDoubleMastery,
            "cooking_double_mastery" => SpecialAttribute.CookingDoubleMastery,
            "improve_chaosraid_wap" => SpecialAttribute.ChaosRaidAttack,
            "improve_chaosraid_asp" => SpecialAttribute.ChaosRaidAttackSpeed,
            "improve_chaosraid_atp" => SpecialAttribute.ChaosRaidAccuracy,
            "improve_chaosraid_hp" => SpecialAttribute.ChaosRaidHp,
            "improve_recovery_ball" => SpecialAttribute.RecoveryBall,
            "improve_fieldboss_kill_exp" => SpecialAttribute.FieldBossExp,
            "improve_fieldboss_kill_drop" => SpecialAttribute.FieldBossDropRate,
            "reduce_fieldboss_recive_damage" => SpecialAttribute.ReduceFieldBossReceiveDamage,
            "additionaleffect_95000016" => SpecialAttribute.AdditionalEffect95000016,
            "improve_pettrap_reward" => SpecialAttribute.PetTrapReward,
            "mining_multiaction" => SpecialAttribute.MiningEfficiency,
            "breeding_multiaction" => SpecialAttribute.BreedingEfficiency,
            "gathering_multiaction" => SpecialAttribute.GatheringEfficiency,
            "farming_multiaction" => SpecialAttribute.FarmingEfficiency,
            "reduce_damage_by_targetmaxhp" => SpecialAttribute.ReduceDamageByTargetMaxHp,
            "reduce_meso_revival_fee" => SpecialAttribute.ReduceMesoRevivalFee,
            "improve_riding_run_speed" => SpecialAttribute.RidingRunSpeed,
            "improve_dungeon_reward_meso" => SpecialAttribute.DungeonRewardMeso,
            "improve_shop_buying_meso" => SpecialAttribute.ShopBuyingMeso,
            "improve_itembox_reward_meso" => SpecialAttribute.ItemBoxRewardMeso,
            "reduce_remakeoption_fee" => SpecialAttribute.ReduceRemakeOptionFee,
            "reduce_airtaxi_fee" => SpecialAttribute.ReduceAirTaxiFee,
            "improve_socket_unlock_probability" => SpecialAttribute.SocketUnlockProbability,
            "reduce_gemstone_upgrade_fee" => SpecialAttribute.ReduceGemstoneUpgradeFee,
            "reduce_pet_remakeoption_fee" => SpecialAttribute.ReducePetRemakeOptionFee,
            "improve_riding_speed" => SpecialAttribute.RidingSpeed,
             "improve_survival_kill_exp" => SpecialAttribute.ImproveSurvivalKillExp,
             "improve_survival_time_exp" => SpecialAttribute.ImproveSurvivalTimeExp,
             "offensive_physicaldamage" => SpecialAttribute.OffensivePhysicalDamage,
             "offensive_magicaldamage" => SpecialAttribute.OffensiveMagicalDamage,
            "reduce_gameitem_socket_unlock_fee" => SpecialAttribute.ReduceGameItemSocketUnlockFee,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid SpecialAttribute."),
        };
    }

    public static SpecialAttribute SgiTarget(this SpecialAttribute attribute, int sgiTarget) {
        if (attribute == SpecialAttribute.NormalNpcDamage) {
            return sgiTarget switch {
                2 => SpecialAttribute.LeaderNpcDamage,
                3 => SpecialAttribute.EliteNpcDamage,
                4 => SpecialAttribute.BossNpcDamage,
                _ => SpecialAttribute.NormalNpcDamage,
            };
        }

        return attribute;
    }
}
