using System;
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
            if (value < 0) {
                throw new ArgumentException($"Invalid amount");
            }

            switch (type) {
                case MasteryType.Fishing:
                    Mastery.Fishing = Math.Min(value, Constant.FishingMasteryMax);
                    break;
                case MasteryType.Music: 
                    Mastery.Instrument = Math.Min(value, Constant.PerformanceMasteryMax);
                    break;
                case MasteryType.Mining:
                    session.Player.Value.Character.Mastery.Mining = Math.Min(value, Constant.MiningMasteryMax);
                    break;
                case MasteryType.Gathering:
                    Mastery.Foraging = Math.Min(value, Constant.ForagingMasteryMax);
                    break;
                case MasteryType.Breeding:
                    Mastery.Ranching = Math.Min(value, Constant.RanchingMasteryMax);
                    break;
                case MasteryType.Farming:
                    Mastery.Farming = Math.Min(value, Constant.FarmingMasteryMax);
                    break;
                case MasteryType.Blacksmithing:
                    Mastery.Smithing = Math.Min(value, Constant.SmithingMasteryMax);
                    break;
                case MasteryType.Engraving:
                    Mastery.Handicrafts = Math.Min(value, Constant.HandicraftsMasteryMax);
                    break;
                case MasteryType.Alchemist:
                    Mastery.Alchemy = Math.Min(value, Constant.AlchemyMasteryMax);
                    break;
                case MasteryType.Cooking:
                    Mastery.Cooking = Math.Min(value, Constant.CookingMasteryMax);
                    break;
                case MasteryType.PetTaming:
                    Mastery.PetTaming = Math.Min(value, Constant.PetTamingMasteryMax);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid mastery type.");
            }
        }
    }
}
