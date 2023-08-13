using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager;

public class StatsManager {
    private readonly GameSession session;
    private readonly Lua.Lua lua;

    public readonly Stats Values;

    public StatsManager(GameSession session, Lua.Lua lua) {
        this.session = session;
        this.lua = lua;

        Player player = session.Player;
        Values = new Stats(player.Character.Job.Code(), player.Character.Level);
        AddEquips();
    }

    public void Refresh() {
        Character character = session.Player.Value.Character;

        Values.Reset(character.Job.Code(), character.Level);
        AddEquips();
        AddBuffs();
        Values.Finalize();
        session.Field?.Broadcast(StatsPacket.Init(session.Player));
        session.Field?.Broadcast(StatsPacket.Update(session.Player), session);
    }

    private void AddEquips() {
        Values.GearScore = 0;
        foreach (Item item in session.Item.Equips.Gear.Values) {
            if (item.Stats != null) {
                AddItemStats(item.Stats);
            }
            Values.GearScore += lua.CalcItemLevel(item.Metadata.Property.GearScore, item.Rarity, item.Type.Type, item.Enchant?.Enchants ?? 0, item.LimitBreak?.Level ?? 0).Item1;

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

    private void AddBuffs() {
        foreach (Buff buff in session.Player.Buffs.Buffs.Values) {
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
