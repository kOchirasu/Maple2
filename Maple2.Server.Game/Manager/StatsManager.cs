using System;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Formulas;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Manager;

public class StatsManager {
    private readonly IActor actor;

    public readonly Stats Values;

    public StatsManager(IActor actor) {
        this.actor = actor;

        switch (actor) {
            case FieldPlayer player:
                Values = new Stats(player.Value.Character.Job.Code(), player.Value.Character.Level);
                break;
            case FieldNpc npc:
                Values = new Stats(npc.Value.Metadata.Stat);
                break;
            default:
                Values = new Stats(0, 0);
                break;
        }
    }

    /// <summary>
    /// Get the actor's bonus attack min and max.
    /// </summary>
    /// <param name="targetBonusAttackResistance">Target's total Bonus Attack resistance</param>
    /// <param name="targetMaxWeaponAttackResistance">Target's total Max Weapon Attack resistance</param>
    /// <returns>Tuple. Item1 = Min Damage. Item2 = Max Damage.</returns>
    public (double, double) GetBonusAttack(float targetBonusAttackResistance, float targetMaxWeaponAttackResistance) {
        switch (actor) {
            case FieldPlayer player:
                double bonusAttack = Values[BasicAttribute.BonusAtk].Total + Constant.PetAttackMultiplier * Values[BasicAttribute.PetBonusAtk].Total;
                double bonusAttackResistance = 1 / (1 + targetBonusAttackResistance);
                double weaponAttackWeakness = 1 / (1 + targetMaxWeaponAttackResistance);
                double bonusAttackCoefficient = bonusAttackResistance * BonusAttackCoefficient(player);
                double minDamage = weaponAttackWeakness * Values[BasicAttribute.MinWeaponAtk].Total + bonusAttackCoefficient * bonusAttack;
                double maxDamage = weaponAttackWeakness * Values[BasicAttribute.MaxWeaponAtk].Total + bonusAttackCoefficient * bonusAttack;
                return (minDamage, maxDamage);
            case FieldNpc npc:
                return (1, 1);
        }
        return (1, 1);
        
        double BonusAttackCoefficient(FieldPlayer player) {
            int leftHandRarity = player.Session.Item.Equips.Get(EquipSlot.RH)?.Rarity ?? 0;
            int rightHandRarity = player.Session.Item.Equips.Get(EquipSlot.LH)?.Rarity ?? 0;
            return BonusAttack.Coefficient(rightHandRarity, leftHandRarity, player.Value.Character.Job.Code());
        }
    }
    
    /// <summary>
    /// Get actor's critical damage result. Takes into consideration the target's critical damage resistance.
    /// </summary>
    /// <param name="targetCriticalDamageResistance">Target's critical damage resistance</param>
    /// <param name="mode">Unknown. Used for Lua script. If 0, the cap of damage is 250%. If 14, it's 300%</param>
    /// <returns>Critical damage</returns>
    public float GetCriticalDamage(float targetCriticalDamageResistance, int mode = 0) {
        float criticalDamage = actor.Field.Lua.CalcCritDamage(Values[BasicAttribute.CriticalDamage].Total, mode);
        //TODO: Apply target's resistance. Need to figure out formula for this.
        return criticalDamage;
    }

    /// <summary>
    /// Gets the critical rate of the actor - uses a different formula for players and NPCs.
    /// </summary>
    /// <param name="targetCriticalEvasion"></param>
    /// <param name="casterCriticalOverride"></param>
    /// <returns>DamageType. If successful crit, returns DamageType.Critical. else returns DamageType.Normal</returns>
    public DamageType GetCriticalRate(long targetCriticalEvasion, double casterCriticalOverride) {
        float criticalChance = actor switch {
            FieldPlayer player => actor.Field.Lua.CalcPlayerCritRate((int) player.Value.Character.Job.Code(), player.Stats.Values[BasicAttribute.Luck].Total, player.Stats.Values[BasicAttribute.CriticalRate].Total, targetCriticalEvasion, 0, 0),
            FieldNpc npc => actor.Field.Lua.CalcNpcCritRate(npc.Stats.Values[BasicAttribute.Luck].Total, npc.Stats.Values[BasicAttribute.CriticalRate].Total, targetCriticalEvasion),
            _ => 0
        };

        return Random.Shared.NextDouble() < Math.Max(criticalChance, casterCriticalOverride) ? DamageType.Critical : DamageType.Normal;
    }

    /// <summary>
    /// Refresh the stats of the actor. This is used when the actor's stats are changed.
    /// Will only process if actor is a player.
    /// </summary>
    public void Refresh() {
        if (actor is not FieldPlayer player) {
            return;
        }
        Character character = player.Value.Character;

        Values.Reset(character.Job.Code(), character.Level);
        AddEquips(player);
        AddBuffs(player);
        Values.Total();
        actor.Field.Broadcast(StatsPacket.Init(player));
        actor.Field.Broadcast(StatsPacket.Update(player), player.Session);
    }

    private void AddEquips(FieldPlayer player) {
        Values.GearScore = 0;
        foreach (Item item in player.Session.Item.Equips.Gear.Values) {
            if (item.Stats != null) {
                AddItemStats(item.Stats);
            }
            Values.GearScore += actor.Field.Lua.CalcItemLevel(item.Metadata.Property.GearScore, item.Rarity, item.Type.Type, item.Enchant?.Enchants ?? 0, item.LimitBreak?.Level ?? 0).Item1;

            if (item.Socket != null) {
                for (int index = 0; index < item.Socket.UnlockSlots; index++) {
                    ItemGemstone? gem = item.Socket.Sockets[index];
                    if (gem != null && gem.Stats != null) {
                        AddItemStats(gem.Stats);
                    }
                }
            }
        }
    }

    private void AddBuffs(FieldPlayer player) {
        foreach (Buff buff in player.Session.Player.Buffs.Buffs.Values) {
            foreach ((BasicAttribute valueBasicAttribute, long value) in buff.Metadata.Status.Values) {
                Values[valueBasicAttribute].AddTotal(value);
            }
            foreach ((BasicAttribute ratespecialAttribute, float rate) in buff.Metadata.Status.Rates) {
                Values[ratespecialAttribute].AddRate(rate);
            }
            foreach ((SpecialAttribute valueSpecialAttribute, float value) in buff.Metadata.Status.SpecialValues) {
                Values[valueSpecialAttribute].AddTotal((long) value);
            }
            foreach ((SpecialAttribute rateSpecialAttribute, float rate) in buff.Metadata.Status.SpecialRates) {
                Values[rateSpecialAttribute].AddRate(rate);
            }
        }
    }

    private void AddItemStats(ItemStats stats) {
        for (int type = 0; type < ItemStats.TYPE_COUNT; type++) {
            foreach ((BasicAttribute attribute, BasicOption option) in stats[(ItemStats.Type) type].Basic) {
                Values[attribute].AddTotal(option);
            }

            foreach ((SpecialAttribute attribute, SpecialOption option) in stats[(ItemStats.Type) type].Special) {
                Values[attribute].AddTotal(option);
            }
        }
    }
}
