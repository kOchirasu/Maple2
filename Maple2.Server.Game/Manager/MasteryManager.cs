﻿using System;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager;

public class MasteryManager {
    private readonly GameSession session;
    
    private Mastery Mastery => session.Player.Value.Character.Mastery;
    public MasteryManager(GameSession session) {
        this.session = session;
    }
    
    public int this[MasteryType type] {
        get => type switch {
            MasteryType.Fishing => Mastery.Fishing,
            MasteryType.Music => Mastery.Instrument,
            MasteryType.Mining => Mastery.Mining,
            MasteryType.Gathering => Mastery.Foraging,
            MasteryType.Breeding => Mastery.Ranching,
            MasteryType.Farming => Mastery.Farming,
            MasteryType.Blacksmithing => Mastery.Smithing,
            MasteryType.Engraving => Mastery.Handicrafts,
            MasteryType.Alchemist => Mastery.Alchemy,
            MasteryType.Cooking => Mastery.Cooking,
            MasteryType.PetTaming => Mastery.PetTaming,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid mastery type.")
        };
        set {
            switch (type) {
                case MasteryType.Fishing:
                    Mastery.Fishing = Math.Clamp(value, Mastery.Fishing, Constant.FishingMasteryMax);
                    break;
                case MasteryType.Music: 
                    Mastery.Instrument = Math.Clamp(value, Mastery.Instrument, Constant.PerformanceMasteryMax);
                    break;
                case MasteryType.Mining:
                    Mastery.Mining = Math.Clamp(value, Mastery.Mining, Constant.MiningMasteryMax);
                    break;
                case MasteryType.Gathering:
                    Mastery.Foraging = Math.Clamp(value, Mastery.Foraging, Constant.ForagingMasteryMax);
                    break;
                case MasteryType.Breeding:
                    Mastery.Ranching = Math.Clamp(value, Mastery.Ranching, Constant.RanchingMasteryMax);
                    break;
                case MasteryType.Farming:
                    Mastery.Farming = Math.Clamp(value, Mastery.Farming, Constant.FarmingMasteryMax);
                    break;
                case MasteryType.Blacksmithing:
                    Mastery.Smithing = Math.Clamp(value, Mastery.Smithing, Constant.SmithingMasteryMax);
                    break;
                case MasteryType.Engraving:
                    Mastery.Handicrafts = Math.Clamp(value, Mastery.Handicrafts, Constant.HandicraftsMasteryMax);
                    break;
                case MasteryType.Alchemist:
                    Mastery.Alchemy = Math.Clamp(value, Mastery.Alchemy, Constant.AlchemyMasteryMax);
                    break;
                case MasteryType.Cooking:
                    Mastery.Cooking = Math.Clamp(value, Mastery.Cooking, Constant.CookingMasteryMax);
                    break;
                case MasteryType.PetTaming:
                    Mastery.PetTaming = Math.Clamp(value, Mastery.PetTaming, Constant.PetTamingMasteryMax);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid mastery type.");
            }
            
            session.Send(MasteryPacket.UpdateMastery(type, session.Mastery[type]));
        }
    }
}
